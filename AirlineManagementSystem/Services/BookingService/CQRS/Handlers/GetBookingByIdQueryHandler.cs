using BookingService.CQRS.Queries;
using BookingService.DTOs;
using BookingService.Repositories;

namespace BookingService.CQRS.Handlers;

public class GetBookingByIdQueryHandler
{
    private readonly IBookingRepository _repository;
    private readonly ILogger<GetBookingByIdQueryHandler> _logger;

    public GetBookingByIdQueryHandler(
        IBookingRepository repository,
        ILogger<GetBookingByIdQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<BookingDto> HandleAsync(GetBookingByIdQuery query)
    {
        var booking = await _repository.GetByIdAsync(query.BookingId);
        if (booking == null)
            throw new KeyNotFoundException($"Booking {query.BookingId} not found");

        _logger.LogInformation($"Retrieved booking {query.BookingId}");

        return MapToDto(booking);
    }

    private BookingDto MapToDto(BookingService.Models.Booking booking)
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
