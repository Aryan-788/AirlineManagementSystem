using Shared.Models;

namespace AgentService.Models;

public class Dealer : BaseEntity
{
    public string DealerName { get; set; } = "";
    public string DealerEmail { get; set; } = "";
    public int AllocatedSeats { get; set; }
    public int UsedSeats { get; set; }
    public decimal CommissionRate { get; set; }
    public bool IsActive { get; set; }
}

public class DealerBooking : BaseEntity
{
    public int DealerId { get; set; }
    public int BookingId { get; set; }
    public int FlightId { get; set; }
    public decimal Commission { get; set; }
}
