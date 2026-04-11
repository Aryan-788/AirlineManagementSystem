namespace PaymentService.DTOs;

public class RefundRequestDto
{
    public int BookingId { get; set; }
    public int? PassengerId { get; set; }
    public decimal Amount { get; set; }
}
