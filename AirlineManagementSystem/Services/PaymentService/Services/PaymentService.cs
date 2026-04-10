using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Repositories;
using Shared.Events;
using Shared.Models;
using Shared.RabbitMQ;
using Razorpay.Api;
using System.Collections.Generic;

namespace PaymentService.Services;

public interface IPaymentService
{
    Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto dto);
    Task<object> CreateOrderAsync(CreateOrderDto dto);
    Task<PaymentDto> VerifySignatureAsync(VerifySignatureDto dto);
    Task<PaymentDto> GetPaymentAsync(int id);
    Task<PaymentDto> RefundAsync(int paymentId);
    Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync();
}

public class PaymentServiceImpl : IPaymentService
{
    private readonly IPaymentRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PaymentServiceImpl> _logger;

    public PaymentServiceImpl(
        IPaymentRepository repository,
        IEventPublisher eventPublisher,
        HttpClient httpClient,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PaymentServiceImpl> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _httpClient = httpClient;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private async Task ValidateBookingAsync(int bookingId)
    {
        try
        {
            var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            var bookingServiceUrl = _configuration["ServiceUrls:BookingService"];
            if (string.IsNullOrEmpty(bookingServiceUrl))
            {
                _logger.LogWarning("BookingService URL not configured, skipping validation");
                return;
            }

            var requestMessage = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, $"{bookingServiceUrl}/api/bookings/{bookingId}");
            if (!string.IsNullOrEmpty(token))
            {
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            _logger.LogInformation("Validating booking {BookingId} against {Url}", bookingId, bookingServiceUrl);
            var bookingResponse = await _httpClient.SendAsync(requestMessage);
            if (!bookingResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Booking validation failed: {StatusCode}", bookingResponse.StatusCode);
                throw new InvalidOperationException($"Booking {bookingId} does not exist or is not accessible");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Unable to validate booking {BookingId}", bookingId);
            throw new InvalidOperationException($"Unable to validate booking: {ex.Message}");
        }
    }

    public async Task<object> CreateOrderAsync(CreateOrderDto dto)
    {
        _logger.LogInformation("CreateOrderAsync called for BookingId={BookingId}, Amount={Amount}", dto.BookingId, dto.Amount);

        // 1. Validate Booking
        await ValidateBookingAsync(dto.BookingId);

        // 2. Initialize RazorPay Client
        string? key = _configuration["Razorpay:KeyId"];
        string? secret = _configuration["Razorpay:KeySecret"];

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(secret))
        {
            _logger.LogError("Razorpay KeyId or KeySecret is not configured");
            throw new InvalidOperationException("Razorpay payment gateway is not configured. Contact admin.");
        }

        // 3. Validate amount
        if (dto.Amount <= 0)
        {
            _logger.LogError("Invalid amount {Amount} for BookingId={BookingId}", dto.Amount, dto.BookingId);
            throw new InvalidOperationException($"Invalid payment amount: {dto.Amount}. Amount must be greater than 0.");
        }

        // 4. Define RazorPay Order Options
        int amountInPaise = (int)(dto.Amount * 100);
        _logger.LogInformation("Creating Razorpay order: BookingId={BookingId}, Amount={Amount}, AmountInPaise={Paise}", dto.BookingId, dto.Amount, amountInPaise);
        Dictionary<string, object> options = new Dictionary<string, object>();
        options.Add("amount", amountInPaise); // amount in paise MUST be int
        options.Add("currency", "INR");
        options.Add("receipt", $"booking_rcptid_{dto.BookingId}");

        try
        {
            var client = new RazorpayClient(key, secret);
            Order order = client.Order.Create(options);
            string orderId = order["id"]?.ToString() ?? throw new InvalidOperationException("Razorpay returned null order ID");
            _logger.LogInformation("Razorpay Order created: {OrderId}", orderId);

            // 4. Return full order data to Angular (key, amount, currency, orderId)
            return new
            {
                orderId = orderId,
                key = key,
                amount = amountInPaise,
                currency = "INR"
            };
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Razorpay order creation failed for BookingId={BookingId}", dto.BookingId);
            throw new InvalidOperationException($"Razorpay order creation failed: {ex.Message}");
        }
    }

    public async Task<PaymentDto> VerifySignatureAsync(VerifySignatureDto dto)
    {
        _logger.LogInformation("VerifySignatureAsync called for BookingId={BookingId}", dto.BookingId);

        string key = _configuration["Razorpay:KeyId"]!;
        string secret = _configuration["Razorpay:KeySecret"]!;

        // Initialize RazorpayClient to ensure static key/secret are set for Utils.verifyPaymentSignature
        var client = new RazorpayClient(key, secret);

        Dictionary<string, string> attributes = new Dictionary<string, string>();
        attributes.Add("razorpay_payment_id", dto.RazorpayPaymentId);
        attributes.Add("razorpay_order_id", dto.RazorpayOrderId);
        attributes.Add("razorpay_signature", dto.RazorpaySignature);

        try
        {
            Utils.verifyPaymentSignature(attributes);
            _logger.LogInformation("Razorpay signature verified successfully for BookingId={BookingId}", dto.BookingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Razorpay signature verification failed for BookingId={BookingId}", dto.BookingId);

            // Publish PaymentFailedEvent so Saga can cancel the booking
            await _eventPublisher.PublishAsync(new PaymentFailedEvent(
                0,
                dto.BookingId,
                dto.UserId,
                "Invalid RazorPay Signature",
                DateTime.UtcNow));

            throw new InvalidOperationException("Invalid RazorPay Signature. Payment Failed.");
        }

        // Signature valid, register payment with actual amount from DTO
        var payment = new PaymentService.Models.Payment
        {
            BookingId = dto.BookingId,
            Amount = dto.Amount,
            PaymentMethod = "RazorPay",
            TransactionId = dto.RazorpayPaymentId,
            Status = PaymentStatus.Success,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(payment);
        _logger.LogInformation("Payment record saved: PaymentId={PaymentId}, Amount={Amount}", payment.Id, dto.Amount);

        // Publish PaymentSuccessEvent so Saga can confirm the booking and trigger rewards
        await _eventPublisher.PublishAsync(new PaymentSuccessEvent(
            payment.Id,
            dto.BookingId,
            dto.UserId,
            dto.Amount,
            DateTime.UtcNow));

        _logger.LogInformation("PaymentSuccessEvent published for BookingId={BookingId}", dto.BookingId);

        return MapToDto(payment);
    }

    public async Task<PaymentDto> ProcessPaymentAsync(ProcessPaymentDto dto)
    {
        // Validate booking exists
        await ValidateBookingAsync(dto.BookingId);

        var transactionId = Guid.NewGuid().ToString();

        var payment = new PaymentService.Models.Payment
        {
            BookingId = dto.BookingId,
            Amount = dto.Amount,
            PaymentMethod = dto.PaymentMethod,
            TransactionId = transactionId,
            Status = PaymentStatus.Success,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(payment);

        await _eventPublisher.PublishAsync(new PaymentSuccessEvent(
            payment.Id,
            dto.BookingId,
            dto.UserId,
            dto.Amount,
            DateTime.UtcNow));

        return MapToDto(payment);
    }

    public async Task<PaymentDto> GetPaymentAsync(int id)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null)
            throw new KeyNotFoundException($"Payment {id} not found");

        return MapToDto(payment);
    }

    public async Task<PaymentDto> RefundAsync(int paymentId)
    {
        var payment = await _repository.GetByIdAsync(paymentId);
        if (payment == null)
            throw new KeyNotFoundException($"Payment {paymentId} not found");

        payment.Status = PaymentStatus.Refunded;
        await _repository.UpdateAsync(payment);

        return MapToDto(payment);
    }

    private PaymentDto MapToDto(PaymentService.Models.Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            PaymentMethod = payment.PaymentMethod,
            CreatedAt = payment.CreatedAt
        };
    }

    public async Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync()
    {
        var payments = await _repository.GetAllAsync();
        return payments.Select(MapToDto);
    }
}
