namespace BookingService.DTOs;

public class RefundDetailsDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string PNR { get; set; } = string.Empty;
    public int? PassengerId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string passengerAadhar { get; set; } = string.Empty;
    public string CancellationReason { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public decimal RefundPercentage { get; set; }
    public string RefundStatus { get; set; } = string.Empty;
    public DateTime CancellationTime { get; set; }
    public DateTime DepartureTime { get; set; }
}
