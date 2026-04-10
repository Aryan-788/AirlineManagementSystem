using BookingService.CQRS.Commands;
using BookingService.Repositories;
using Shared.Events;
using Shared.RabbitMQ;

namespace BookingService.CQRS.Handlers;

public class CancelBookingCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CancelBookingCommandHandler> _logger;

    public CancelBookingCommandHandler(
        IBookingRepository repository,
        IEventPublisher eventPublisher,
        ILogger<CancelBookingCommandHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(CancelBookingCommand command)
    {
        var booking = await _repository.GetByIdAsync(command.BookingId);
        if (booking == null)
            throw new KeyNotFoundException($"Booking {command.BookingId} not found");

        booking.Status = Shared.Models.BookingStatus.Cancelled;
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

        _logger.LogInformation($"Booking {command.BookingId} cancelled successfully");
    }
}
