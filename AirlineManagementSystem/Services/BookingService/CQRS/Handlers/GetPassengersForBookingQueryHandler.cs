using BookingService.CQRS.Queries;
using BookingService.DTOs;
using BookingService.Repositories;

namespace BookingService.CQRS.Handlers;

public class GetPassengersForBookingQueryHandler
{
    private readonly IPassengerRepository _repository;
    private readonly ILogger<GetPassengersForBookingQueryHandler> _logger;

    public GetPassengersForBookingQueryHandler(
        IPassengerRepository repository,
        ILogger<GetPassengersForBookingQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<PassengerResponseDto>> HandleAsync(GetPassengersForBookingQuery query)
    {
        var passengers = await _repository.GetPassengersByBookingIdAsync(query.BookingId);

        _logger.LogInformation($"Retrieved {passengers.Count} passengers for booking {query.BookingId}");

        return passengers.Select(MapToResponseDto).ToList();
    }

    private PassengerResponseDto MapToResponseDto(BookingService.Models.Passenger passenger)
    {
        return new PassengerResponseDto
        {
            Id = passenger.Id,
            Name = passenger.Name,
            Age = passenger.Age,
            Gender = passenger.Gender,
            AadharCardNo = passenger.AadharCardNo,
            Status = passenger.Status.ToString(),
            SeatNumber = passenger.SeatNumber,
            CancelledAt = passenger.CancelledAt,
            CancellationReason = passenger.CancellationReason,
            CreatedAt = passenger.CreatedAt
        };
    }
}
