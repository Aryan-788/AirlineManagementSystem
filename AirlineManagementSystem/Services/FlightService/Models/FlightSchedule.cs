using Shared.Models;

namespace FlightService.Models;

public class FlightSchedule : BaseEntity
{
    public int FlightId { get; set; }
    public Flight? Flight { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string Gate { get; set; } = "";
    public FlightStatus Status { get; set; } = FlightStatus.Scheduled;
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int EconomySeats { get; set; }
    public int BusinessSeats { get; set; }
    public int FirstSeats { get; set; }
    
    // Pricing for different seat classes per schedule
    public decimal EconomyPrice { get; set; }
    public decimal BusinessPrice { get; set; }
    public decimal FirstClassPrice { get; set; }
}
