using Shared.Events;

namespace BookingService.CQRS.Commands;

public class HandlePaymentSuccessCommand
{
    public PaymentSuccessEvent Event { get; set; }

    public HandlePaymentSuccessCommand(PaymentSuccessEvent @event)
    {
        Event = @event;
    }
}
