using BookingService.CQRS.Commands;
using BookingService.Repositories;
using Shared.Events;
using Shared.Models;
using Shared.RabbitMQ;

namespace BookingService.CQRS.Handlers;

public class HandlePaymentSuccessCommandHandler
{
    private readonly IBookingRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<HandlePaymentSuccessCommandHandler> _logger;

    public HandlePaymentSuccessCommandHandler(
        IBookingRepository repository,
        IEventPublisher eventPublisher,
        ILogger<HandlePaymentSuccessCommandHandler> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task HandleAsync(HandlePaymentSuccessCommand command)
    {
        try
        {
            var paymentEvent = command.Event;
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

            _logger.LogInformation($"Payment success handled for booking {paymentEvent.BookingId}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error handling payment success for booking {command.Event.BookingId}: {ex.Message}");
        }
    }
}
