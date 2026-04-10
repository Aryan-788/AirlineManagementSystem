using BookingService.DTOs;
using BookingService.Models;
using BookingService.Repositories;
using Shared.Events;
using Shared.Models;
using Shared.RabbitMQ;

namespace BookingService.Services;

public class FlightVerificationDto
{
    public string status { get; set; } = "";
    public DateTime departureTime { get; set; }
    public int economySeats { get; set; }
    public int businessSeats { get; set; }
    public int firstSeats { get; set; }
}

public class ScheduleVerificationDto
{
    public string status { get; set; } = "";
    public DateTime departureTime { get; set; }
    public int economySeats { get; set; }
    public int businessSeats { get; set; }
    public int firstSeats { get; set; }
}

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(CreateBookingDto dto);
    Task<BookingDto> GetBookingAsync(int id);
    Task CancelBookingAsync(int id);
    Task<IEnumerable<BookingHistoryDto>> GetBookingHistoryAsync(int userId);
    Task<IEnumerable<object>> GetBookingsByScheduleAsync(int scheduleId);
    Task<IEnumerable<string>> GetOccupiedSeatsAsync(int flightId, int? scheduleId);
    Task<BookingDto> UpdateBookingAsync(int id, Booking booking);
    Task HandlePaymentSuccessAsync(PaymentSuccessEvent paymentEvent);
    Task HandlePaymentFailedAsync(PaymentFailedEvent paymentEvent);
}

public class BookingServiceImpl : IBookingService
{
    private readonly IBookingRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly HttpClient _httpClient;
    private readonly ILogger<BookingServiceImpl> _logger;
    private readonly IConfiguration _configuration;

    public BookingServiceImpl(
        IBookingRepository repository, 
        IEventPublisher eventPublisher,
        HttpClient httpClient,
        ILogger<BookingServiceImpl> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<BookingDto> CreateBookingAsync(CreateBookingDto dto)
    {
        _logger.LogInformation($"Starting booking creation for User {dto.UserId}, Flight {dto.FlightId}, Schedule {dto.ScheduleId}");

        var flightServiceUrl = _configuration["ServiceUrls:FlightService"];

        // ── Validate via schedule or flight ──
        try
        {
            if (dto.ScheduleId.HasValue)
            {
                // Schedule-based booking: validate against schedule endpoint
                var scheduleResponse = await _httpClient.GetAsync($"{flightServiceUrl}/api/flights/schedules/{dto.ScheduleId.Value}");
                if (!scheduleResponse.IsSuccessStatusCode)
                    throw new InvalidOperationException("Schedule does not exist or is unavailable");

                var scheduleData = await scheduleResponse.Content.ReadFromJsonAsync<ScheduleVerificationDto>();
                if (scheduleData == null)
                    throw new InvalidOperationException("Failed to read schedule data");

                if (scheduleData.status == "Cancelled" || scheduleData.status == "Completed")
                    throw new InvalidOperationException($"Schedule is {scheduleData.status} and cannot be booked");

                if (scheduleData.departureTime < DateTime.UtcNow)
                    throw new InvalidOperationException("This schedule has already departed");

                int availableSeatsForClass = dto.SeatClass switch
                {
                    "Economy" => scheduleData.economySeats,
                    "Business" => scheduleData.businessSeats,
                    "First" => scheduleData.firstSeats,
                    _ => 0
                };
                if (availableSeatsForClass < dto.PassengerCount)
                    throw new InvalidOperationException($"Not enough {dto.SeatClass} seats. Available: {availableSeatsForClass}, Requested: {dto.PassengerCount}");
            }
            else
            {
                // Legacy flight-based booking
                var response = await _httpClient.GetAsync($"{flightServiceUrl}/api/flights/{dto.FlightId}");
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("Flight does not exist");

                var flightData = await response.Content.ReadFromJsonAsync<FlightVerificationDto>();
                if (flightData == null)
                    throw new InvalidOperationException("Failed to read flight data");

                if (flightData.status == "Cancelled")
                    throw new InvalidOperationException("Flight is cancelled");

                if (flightData.departureTime < DateTime.UtcNow)
                    throw new InvalidOperationException("Flight has already departed");

                int availableSeatsForClass = dto.SeatClass switch
                {
                    "Economy" => flightData.economySeats,
                    "Business" => flightData.businessSeats,
                    "First" => flightData.firstSeats,
                    _ => 0
                };
                if (availableSeatsForClass <= 0)
                    throw new InvalidOperationException($"No available {dto.SeatClass} seats on this flight");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning($"Flight service unavailable: {ex.Message}");
            throw new InvalidOperationException("Flight service unavailable");
        }

        // Validate seat class
        if (!Enum.TryParse<SeatClass>(dto.SeatClass, out var seatClass))
            throw new InvalidOperationException($"Invalid seat class: {dto.SeatClass}. Valid options: {string.Join(", ", Enum.GetNames(typeof(SeatClass)))}");

        if (dto.BaggageWeight < 0 || dto.BaggageWeight > 100)
            throw new InvalidOperationException("Baggage weight must be between 0 and 100 kg");

        if (dto.UserId <= 0)
            throw new InvalidOperationException("User ID must be greater than 0");

        var pnr = GeneratePNR();

        var booking = new Booking
        {
            UserId = dto.UserId,
            FlightId = dto.FlightId,
            ScheduleId = dto.ScheduleId,
            SeatClass = seatClass,
            BaggageWeight = dto.BaggageWeight,
            PNR = pnr,
            Status = BookingStatus.Pending,
            TotalPassengers = 0,
            ConfirmedPassengers = 0,
            CancelledPassengers = 0,
            CreatedAt = DateTime.UtcNow,
            TotalAmount = dto.TotalAmount
        };

        await _repository.AddAsync(booking);

        // Book seat on schedule or flight
        try
        {
            string bookSeatUrl;
            if (dto.ScheduleId.HasValue)
                bookSeatUrl = $"{flightServiceUrl}/api/flights/schedules/{dto.ScheduleId.Value}/book-seat";
            else
                bookSeatUrl = $"{flightServiceUrl}/api/flights/{dto.FlightId}/book-seat";

            var bookSeatContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(new { seatClass = dto.SeatClass, count = dto.PassengerCount }),
                System.Text.Encoding.UTF8,
                "application/json");

            var bookSeatResponse = await _httpClient.PostAsync(bookSeatUrl, bookSeatContent);

            if (!bookSeatResponse.IsSuccessStatusCode)
            {
                await _repository.DeleteAsync(booking.Id);
                throw new InvalidOperationException("Failed to book seat");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to book seat: {ex.Message}");
            await _repository.DeleteAsync(booking.Id);
            throw;
        }

        _logger.LogInformation($"Booking {booking.Id} created with PNR: {pnr}");

        await _eventPublisher.PublishAsync(new BookingCreatedEvent(
            booking.Id, booking.UserId, booking.FlightId,
            booking.SeatClass.ToString(), 0, booking.CreatedAt));

        return MapToDto(booking);
    }

    public async Task<BookingDto> GetBookingAsync(int id)
    {
        var booking = await _repository.GetByIdAsync(id);
        if (booking == null)
            throw new KeyNotFoundException($"Booking {id} not found");

        return MapToDto(booking);
    }

    public async Task CancelBookingAsync(int id)
    {
        var booking = await _repository.GetByIdAsync(id);
        if (booking == null)
            throw new KeyNotFoundException($"Booking {id} not found");

        booking.Status = BookingStatus.Cancelled;
        await _repository.UpdateAsync(booking);

        await _eventPublisher.PublishAsync(new BookingCancelledEvent(
            booking.Id,
            booking.UserId,
            booking.FlightId,
            booking.ScheduleId,
            booking.SeatClass.ToString(),
            booking.TotalPassengers > 0 ? booking.TotalPassengers : 1,
            0,
            DateTime.UtcNow));
    }

    public async Task<IEnumerable<BookingHistoryDto>> GetBookingHistoryAsync(int userId)
    {
        var bookings = await _repository.GetByUserIdAsync(userId);
        return bookings.Select(b => new BookingHistoryDto
        {
            Id = b.Id,
            FlightId = b.FlightId,
            PNR = b.PNR,
            Status = b.Status.ToString(),
            CreatedAt = b.CreatedAt,
            TotalAmount = b.TotalAmount
        });
    }

    public async Task<IEnumerable<object>> GetBookingsByScheduleAsync(int scheduleId)
    {
        var bookings = await _repository.GetByScheduleIdAsync(scheduleId);
        return bookings.Select(b => (object)new {
            Id = b.Id,
            PNR = b.PNR,
            UserId = b.UserId,
            SeatClass = b.SeatClass.ToString(),
            Status = b.Status.ToString(),
            PaymentStatus = b.PaymentStatus.ToString(),
            Passengers = b.Passengers?.Select(p => (object)new {
                p.Id,
                p.Name,
                p.Age,
                p.Gender,
                Status = p.Status.ToString(),
                Seat = p.SeatNumber ?? "TBD"
            }).ToList() ?? new List<object>()
        }).ToList();
    }

    public async Task<IEnumerable<string>> GetOccupiedSeatsAsync(int flightId, int? scheduleId)
    {
        return await _repository.GetOccupiedSeatsAsync(flightId, scheduleId);
    }

    public async Task<BookingDto> UpdateBookingAsync(int id, Booking booking)
    {
        var existingBooking = await _repository.GetByIdAsync(id);
        if (existingBooking == null)
            throw new KeyNotFoundException($"Booking {id} not found");

        await _repository.UpdateAsync(booking);
        return MapToDto(booking);
    }

    public async Task HandlePaymentSuccessAsync(PaymentSuccessEvent paymentEvent)
    {
        try
        {
            var booking = await _repository.GetByIdAsync(paymentEvent.BookingId);
            if (booking == null)
            {
                throw new KeyNotFoundException($"Booking {paymentEvent.BookingId} not found");
            }

            // Update booking status to Confirmed
            booking.Status = BookingStatus.Confirmed;
            booking.PaymentStatus = PaymentStatus.Success;
            booking.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(booking);

            // Publish RewardEarnedEvent for reward credit
            await _eventPublisher.PublishAsync(new RewardEarnedEvent(
                booking.UserId,
                100,
                booking.Id,
                DateTime.UtcNow));

            // Also send notification
            // This will be handled by NotificationService subscribing to BookingConfirmedEvent
            // For now, we can publish a custom event or the existing booking confirmed event
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error handling payment success for booking {paymentEvent.BookingId}: {ex.Message}");
        }
    }

    public async Task HandlePaymentFailedAsync(PaymentFailedEvent paymentEvent)
    {
        try
        {
            var booking = await _repository.GetByIdAsync(paymentEvent.BookingId);
            if (booking == null)
            {
                throw new KeyNotFoundException($"Booking {paymentEvent.BookingId} not found");
            }

            // Update booking status to Cancelled
            booking.Status = BookingStatus.Cancelled;
            booking.PaymentStatus = PaymentStatus.Failed;
            booking.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(booking);

            // Publish BookingCancelledEvent for notification
            await _eventPublisher.PublishAsync(new BookingCancelledEvent(
                booking.Id,
                booking.UserId,
                booking.FlightId,
                booking.ScheduleId,
                booking.SeatClass.ToString(),
                booking.TotalPassengers > 0 ? booking.TotalPassengers : 1,
                0,
                DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error handling payment failure for booking {paymentEvent.BookingId}: {ex.Message}");
        }
    }

    private string GeneratePNR()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    private BookingDto MapToDto(Booking booking)
    {
        return new BookingDto
        {
            Id = booking.Id,
            UserId = booking.UserId,
            FlightId = booking.FlightId,
            ScheduleId = booking.ScheduleId,
            SeatClass = booking.SeatClass.ToString(),
            BaggageWeight = booking.BaggageWeight,
            PNR = booking.PNR,
            Status = booking.Status.ToString(),
            PaymentStatus = booking.PaymentStatus.ToString(),
            TotalPassengers = booking.TotalPassengers,
            ConfirmedPassengers = booking.ConfirmedPassengers,
            CancelledPassengers = booking.CancelledPassengers,
            CreatedAt = booking.CreatedAt,
            TotalAmount = booking.TotalAmount
        };
    }
}
