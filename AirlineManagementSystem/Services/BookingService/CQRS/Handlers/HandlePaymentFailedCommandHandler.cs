using BookingService.CQRS.Commands;
using BookingService.Repositories;
using Shared.Events;
using Shared.Models;
using Shared.RabbitMQ;

namespace BookingService.CQRS.Handlers;

public class HandlePaymentFailedCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<HandlePaymentFailedCommandHandler> _logger;

    public HandlePaymentFailedCommandHandler(
        IBookingRepository repository,
        IEventPublisher eventPublisher,
        ILogger<HandlePaymentFailedCommandHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(HandlePaymentFailedCommand command)
    {
        try
        {
            var paymentEvent = command.Event;
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

            // Publish BookingCancelledEvent for notification and Saga compensation
            await _eventPublisher.PublishAsync(new BookingCancelledEvent(
                booking.Id,
                booking.UserId,
                booking.FlightId,
                booking.ScheduleId,
                booking.SeatClass.ToString(),
                booking.TotalPassengers > 0 ? booking.TotalPassengers : 1, // Default to 1 if not set (legacy or edge case)
                0,
                DateTime.UtcNow));

            _logger.LogInformation($"Payment failed handled for booking {paymentEvent.BookingId}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error handling payment failure for booking {command.Event.BookingId}: {ex.Message}");
        }
    }
}
