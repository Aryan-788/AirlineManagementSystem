namespace BookingService.DTOs;

public class CancelMultiplePassengersDto
{
    public List<int> PassengerIds { get; set; } = new List<int>();
    public string? CancellationReason { get; set; }
}
