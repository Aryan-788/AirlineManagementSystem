using BookingService.CQRS.Queries;
using BookingService.DTOs;
using BookingService.Repositories;

namespace BookingService.CQRS.Handlers;

public class GetBookingByPnrQueryHandler
{
    private readonly IBookingRepository _repository;
    private readonly ILogger<GetBookingByPnrQueryHandler> _logger;

    public GetBookingByPnrQueryHandler(
        IBookingRepository repository,
        ILogger<GetBookingByPnrQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<BookingDto> HandleAsync(GetBookingByPnrQuery query)
    {
        var booking = await _repository.GetByPNRAsync(query.PNR);
        if (booking == null)
            throw new KeyNotFoundException($"Booking with PNR {query.PNR} not found");

        _logger.LogInformation($"Retrieved booking by PNR: {query.PNR}");

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
