namespace PaymentService.DTOs;

public class ProcessPaymentDto
{
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public int UserId { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class RefundDto
{
    public int PaymentId { get; set; }
    public decimal RefundAmount { get; set; }
}

public class CreateOrderDto
{
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
}

public class VerifySignatureDto
{
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string RazorpayOrderId { get; set; } = "";
    public string RazorpayPaymentId { get; set; } = "";
    public string RazorpaySignature { get; set; } = "";
    public int UserId { get; set; }
}
