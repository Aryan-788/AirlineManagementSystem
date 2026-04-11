using BookingService.CQRS.Commands;
using BookingService.Repositories;
using Shared.Events;
using Shared.RabbitMQ;
using BookingService.Services;

namespace BookingService.CQRS.Handlers;

public class CancelBookingCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IRefundService _refundService;
    private readonly ILogger<CancelBookingCommandHandler> _logger;

    public CancelBookingCommandHandler(
        IBookingRepository repository,
        IEventPublisher eventPublisher,
        IRefundService refundService,
        ILogger<CancelBookingCommandHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _refundService = refundService;
        _logger = logger;
    }

    public async Task HandleAsync(CancelBookingCommand command)
    {
        var booking = await _repository.GetByIdAsync(command.BookingId);
        if (booking == null)
            throw new KeyNotFoundException($"Booking {command.BookingId} not found");

        booking.Status = Shared.Models.BookingStatus.Cancelled;
        await _repository.UpdateAsync(booking);

        // Process refund for the entire booking
        await _refundService.ProcessRefundAsync(booking, null, booking.TotalPassengers);

        await _eventPublisher.PublishAsync(new BookingCancelledEvent(
            booking.Id,
            booking.UserId,
            booking.FlightId,
            booking.ScheduleId,
            booking.SeatClass.ToString(),
            booking.TotalPassengers > 0 ? booking.TotalPassengers : 1,
            0, // The refund amount is now handled via RefundProcessedEvent
            DateTime.UtcNow));

        _logger.LogInformation($"Booking {command.BookingId} cancelled successfully");
    }
}
