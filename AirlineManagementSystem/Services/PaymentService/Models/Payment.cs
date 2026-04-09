using Shared.Models;

namespace PaymentService.Models;

public class Payment : BaseEntity
{
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string PaymentMethod { get; set; } = "";
    public string TransactionId { get; set; } = "";
}
