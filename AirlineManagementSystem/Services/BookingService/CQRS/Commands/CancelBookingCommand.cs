using BookingService.DTOs;

namespace BookingService.CQRS.Commands;

public class CancelBookingCommand
{
    public int BookingId { get; set; }

    public CancelBookingCommand(int bookingId)
    {
        BookingId = bookingId;
    }
}
