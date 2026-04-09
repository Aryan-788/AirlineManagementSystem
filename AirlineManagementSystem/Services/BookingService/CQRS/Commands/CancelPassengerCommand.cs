using BookingService.DTOs;

namespace BookingService.CQRS.Commands;

public class CancelPassengerCommand
{
    public int PassengerId { get; set; }
    public CancelPassengerDto Dto { get; set; }

    public CancelPassengerCommand(int passengerId, CancelPassengerDto dto)
    {
        PassengerId = passengerId;
        Dto = dto;
    }
}
