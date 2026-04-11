using BookingService.Data;
using BookingService.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Events;
using Shared.RabbitMQ;
using System.Text.Json;
using System.Text;

namespace BookingService.Services;

public interface IRefundService
{
    Task ProcessRefundAsync(Booking booking, int? passengerId, int cancelledPassengerCount);
}

public class RefundService : IRefundService
{
    private readonly BookingDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        BookingDbContext dbContext,
        HttpClient httpClient,
        IConfiguration configuration,
        IEventPublisher eventPublisher,
        ILogger<RefundService> logger)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _configuration = configuration;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task ProcessRefundAsync(Booking booking, int? passengerId, int cancelledPassengerCount)
    {
        try
        {
            var flightServiceUrl = _configuration["ServiceUrls:FlightService"] ?? "http://flight-service:5002";
            var flightDepartureTime = await GetDepartureTimeAsync(booking, flightServiceUrl);

            var hoursBeforeDeparture = (flightDepartureTime - DateTime.UtcNow).TotalHours;

            var policies = await _dbContext.RefundPolicies.ToListAsync();
            var policy = policies.OrderByDescending(p => p.MinHoursBeforeDeparture).FirstOrDefault(p => hoursBeforeDeparture >= p.MinHoursBeforeDeparture && hoursBeforeDeparture <= p.MaxHoursBeforeDeparture);

            decimal refundPercentage = policy?.RefundPercentage ?? 0;

            // Calculate refund amount per passenger
            decimal amountPerPassenger = booking.TotalPassengers > 0 ? booking.TotalAmount / booking.TotalPassengers : 0;
            decimal totalAmountToRefund = amountPerPassenger * cancelledPassengerCount;
            decimal finalRefundAmount = totalAmountToRefund * (refundPercentage / 100m);

            // Bug Fix #1: Non-fatal PaymentService HTTP call.
            // If the call fails (e.g., wrong service URL), we log a warning and continue.
            // The passenger cancellation in DB has already happened and must not be rolled back.
            if (finalRefundAmount > 0)
            {
                var paymentServiceUrl = _configuration["ServiceUrls:PaymentService"] ?? "http://payment-service:5004";
                var refundRequest = new
                {
                    bookingId = booking.Id,
                    passengerId = passengerId,
                    amount = finalRefundAmount
                };

                try
                {
                    var content = new StringContent(JsonSerializer.Serialize(refundRequest), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{paymentServiceUrl}/api/payments/refund", content);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning($"Payment API returned non-success for Booking {booking.Id}: {response.StatusCode}. Refund record will still be created.");
                    }
                }
                catch (Exception httpEx)
                {
                    // Log but DO NOT rethrow — passenger cancellation must succeed
                    _logger.LogWarning($"Could not reach PaymentService for Booking {booking.Id}: {httpEx.Message}. Refund record will still be created.");
                }
            }

            // Bug Fix #3 & #4: Set status to 'RefundPending' (not Success).
            // RefundProcessedEvent is ALWAYS fired — even for 0% refunds — so user always gets notification.
            var refund = new Refund
            {
                BookingId = booking.Id,
                PassengerId = passengerId,
                UserId = booking.UserId,
                RefundAmount = finalRefundAmount,
                RefundPercentage = refundPercentage,
                CancellationTime = DateTime.UtcNow,
                DepartureTime = flightDepartureTime,
                RefundStatus = "RefundPending"  // Fix #4: was hardcoded to "Success"
            };

            await _dbContext.Refunds.AddAsync(refund);
            await _dbContext.SaveChangesAsync();

            // Fix #3: Always publish the event so Notification Service sends the email
            await _eventPublisher.PublishAsync(new RefundProcessedEvent(
                booking.Id,
                passengerId,
                booking.UserId,
                finalRefundAmount,
                refundPercentage,
                DateTime.UtcNow
            ));

            _logger.LogInformation($"Refund processed for Booking {booking.Id}. Amount: {finalRefundAmount}, Percentage: {refundPercentage}%, Status: RefundPending");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing refund for Booking {booking.Id}");
            throw;
        }
    }

    private async Task<DateTime> GetDepartureTimeAsync(Booking booking, string flightServiceUrl)
    {
        try
        {
            if (booking.ScheduleId.HasValue)
            {
                var response = await _httpClient.GetAsync($"{flightServiceUrl}/api/flights/schedules/{booking.ScheduleId.Value}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(content);
                    if (data.TryGetProperty("departureTime", out var depTimeProp))
                    {
                        return DateTime.Parse(depTimeProp.GetString() ?? DateTime.UtcNow.ToString());
                    }
                }
            }
            else
            {
                var response = await _httpClient.GetAsync($"{flightServiceUrl}/api/flights/{booking.FlightId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(content);
                    if (data.TryGetProperty("departureTime", out var depTimeProp))
                    {
                        return DateTime.Parse(depTimeProp.GetString() ?? DateTime.UtcNow.ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not fetch departure time for Flight {booking.FlightId}: {ex.Message}");
        }

        // Return a default past time if not found so refund is 0%
        return DateTime.UtcNow.AddDays(-1);
    }
}
