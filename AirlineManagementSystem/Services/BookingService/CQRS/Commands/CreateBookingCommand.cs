using BookingService.DTOs;

namespace BookingService.CQRS.Commands;

public class CreateBookingCommand
{
    public CreateBookingDto Dto { get; set; }

    public CreateBookingCommand(CreateBookingDto dto)
    {
        Dto = dto;
    }
}
