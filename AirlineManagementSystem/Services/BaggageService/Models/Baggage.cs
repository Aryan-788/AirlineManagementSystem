using Shared.Models;

namespace BaggageService.Models;

public class Baggage : BaseEntity
{
    public int BookingId { get; set; }
    public decimal Weight { get; set; }
    public string PassengerName { get; set; } = "";
    public string FlightNumber { get; set; } = "";
    public BaggageStatus Status { get; set; } = BaggageStatus.Checked;
    public bool IsDelivered { get; set; }
    public string TrackingNumber { get; set; } = "";
}
