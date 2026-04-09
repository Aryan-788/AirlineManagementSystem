using Shared.Events;

namespace BookingService.CQRS.Commands;

public class HandlePaymentFailedCommand
{
    public PaymentFailedEvent Event { get; set; }

    public HandlePaymentFailedCommand(PaymentFailedEvent @event)
    {
        Event = @event;
    }
}
