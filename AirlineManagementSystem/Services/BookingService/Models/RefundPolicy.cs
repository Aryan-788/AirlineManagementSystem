using Shared.Models;

namespace BookingService.Models;

public class RefundPolicy
{
    public int Id { get; set; }
    public double MinHoursBeforeDeparture { get; set; }
    public double MaxHoursBeforeDeparture { get; set; }
    public decimal RefundPercentage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
