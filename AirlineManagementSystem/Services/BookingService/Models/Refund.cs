using Shared.Models;

namespace BookingService.Models;

public class Refund
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int? PassengerId { get; set; }
    public int UserId { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal RefundPercentage { get; set; }
    public DateTime CancellationTime { get; set; }
    public DateTime DepartureTime { get; set; }
    public string RefundStatus { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Booking Booking { get; set; }
}
