using Shared.Models;

namespace RewardService.Models;

public class Reward : BaseEntity
{
    public int UserId { get; set; }
    public int Points { get; set; }
    public string TransactionType { get; set; } = "";
    public int? BookingId { get; set; }
}
