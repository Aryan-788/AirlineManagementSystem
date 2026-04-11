using BookingService.DTOs;

namespace BookingService.CQRS.Commands;

public class CancelMultiplePassengersCommand
{
    public int BookingId { get; set; }
    public CancelMultiplePassengersDto Dto { get; set; }

    public CancelMultiplePassengersCommand(int bookingId, CancelMultiplePassengersDto dto)
    {
        BookingId = bookingId;
        Dto = dto;
    }
}
