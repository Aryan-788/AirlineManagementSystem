using BookingService.CQRS.Commands;
using BookingService.DTOs;
using BookingService.Models;
using BookingService.Repositories;
using Shared.Events;
using Shared.Models;
using Shared.RabbitMQ;
using System.Text.Json;

namespace BookingService.CQRS.Handlers;

public class CreateBookingCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CreateBookingCommandHandler> _logger;
    private readonly IConfiguration _configuration;

    public CreateBookingCommandHandler(
        IBookingRepository repository,
        IEventPublisher eventPublisher,
        HttpClient httpClient,
        ILogger<CreateBookingCommandHandler> logger,
        IConfiguration configuration)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<BookingDto> HandleAsync(CreateBookingCommand command)
    {
        var dto = command.Dto;

        _logger.LogInformation($"Starting booking creation for User {dto.UserId}, Flight {dto.FlightId}, Schedule {dto.ScheduleId}");

        // Validate seat class
        if (!Enum.TryParse<SeatClass>(dto.SeatClass, out var seatClass))
            throw new InvalidOperationException($"Invalid seat class: {dto.SeatClass}. Valid options: {string.Join(", ", Enum.GetNames(typeof(SeatClass)))}");

        if (dto.BaggageWeight < 0 || dto.BaggageWeight > 100)
            throw new InvalidOperationException("Baggage weight must be between 0 and 100 kg");

        if (dto.UserId <= 0)
            throw new InvalidOperationException("User ID must be greater than 0");

        var flightServiceUrl = _configuration["ServiceUrls:FlightService"];

        // Define DTOs locally for validation
        var flightData = await ValidateFlightOrScheduleAsync(dto, flightServiceUrl);

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

    private async Task<JsonElement> ValidateFlightOrScheduleAsync(CreateBookingDto dto, string flightServiceUrl)
    {
        try
        {
            if (dto.ScheduleId.HasValue)
            {
                var scheduleResponse = await _httpClient.GetAsync($"{flightServiceUrl}/api/flights/schedules/{dto.ScheduleId.Value}");
                if (!scheduleResponse.IsSuccessStatusCode)
                    throw new InvalidOperationException("Schedule does not exist or is unavailable");

                var content = await scheduleResponse.Content.ReadAsStringAsync();
                var scheduleData = JsonSerializer.Deserialize<JsonElement>(content);

                if (!scheduleData.TryGetProperty("status", out var statusProp) || statusProp.ValueKind == JsonValueKind.Null)
                {
                    throw new InvalidOperationException("Failed to read schedule data");
                }

                var status = statusProp.GetString() ?? "";
                if (status == "Cancelled" || status == "Completed")
                    throw new InvalidOperationException($"Schedule is {status} and cannot be booked");

                if (!scheduleData.TryGetProperty("departureTime", out var depTimeProp))
                    throw new InvalidOperationException("Schedule missing departure time");

                var departureTime = DateTime.Parse(depTimeProp.GetString() ?? DateTime.UtcNow.ToString());
                if (departureTime < DateTime.UtcNow)
                    throw new InvalidOperationException("This schedule has already departed");

                int availableSeatsForClass = dto.SeatClass switch
                {
                    "Economy" => GetIntProperty(scheduleData, "economySeats"),
                    "Business" => GetIntProperty(scheduleData, "businessSeats"),
                    "First" => GetIntProperty(scheduleData, "firstSeats"),
                    _ => 0
                };
                if (availableSeatsForClass < dto.PassengerCount)
                    throw new InvalidOperationException($"Not enough {dto.SeatClass} seats. Available: {availableSeatsForClass}, Requested: {dto.PassengerCount}");

                return scheduleData;
            }
            else
            {
                var response = await _httpClient.GetAsync($"{flightServiceUrl}/api/flights/{dto.FlightId}");
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("Flight does not exist");

                var content = await response.Content.ReadAsStringAsync();
                var flightData = JsonSerializer.Deserialize<JsonElement>(content);

                if (!flightData.TryGetProperty("status", out var statusProp) || statusProp.ValueKind == JsonValueKind.Null)
                {
                    throw new InvalidOperationException("Failed to read flight data");
                }

                var status = statusProp.GetString() ?? "";
                if (status == "Cancelled")
                    throw new InvalidOperationException("Flight is cancelled");

                if (!flightData.TryGetProperty("departureTime", out var depTimeProp))
                    throw new InvalidOperationException("Flight missing departure time");

                var departureTime = DateTime.Parse(depTimeProp.GetString() ?? DateTime.UtcNow.ToString());
                if (departureTime < DateTime.UtcNow)
                    throw new InvalidOperationException("Flight has already departed");

                int availableSeatsForClass = dto.SeatClass switch
                {
                    "Economy" => GetIntProperty(flightData, "economySeats"),
                    "Business" => GetIntProperty(flightData, "businessSeats"),
                    "First" => GetIntProperty(flightData, "firstSeats"),
                    _ => 0
                };
                if (availableSeatsForClass <= 0)
                    throw new InvalidOperationException($"No available {dto.SeatClass} seats on this flight");

                return flightData;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning($"Flight service unavailable: {ex.Message}");
            throw new InvalidOperationException("Flight service unavailable");
        }
    }

    private int GetIntProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number)
            {
                return prop.GetInt32();
            }
            else if (prop.ValueKind == JsonValueKind.String)
            {
                return int.Parse(prop.GetString() ?? "0");
            }
        }
        return 0;
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
