using Shared.Models;

namespace BookingService.Models;

public class Passenger : BaseEntity
{
    public int BookingId { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Gender { get; set; } = "";
    public string AadharCardNo { get; set; } = "";
    public PassengerStatus Status { get; set; } = PassengerStatus.Confirmed;
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? SeatNumber { get; set; }

    // Navigation property
    public Booking? Booking { get; set; }
}

