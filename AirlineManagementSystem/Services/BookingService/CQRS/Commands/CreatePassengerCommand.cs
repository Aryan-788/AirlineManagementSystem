using BookingService.DTOs;

namespace BookingService.CQRS.Commands;

public class CreatePassengerCommand
{
    public int BookingId { get; set; }
    public CreatePassengerDto Dto { get; set; }

    public CreatePassengerCommand(int bookingId, CreatePassengerDto dto)
    {
        BookingId = bookingId;
        Dto = dto;
    }
}
