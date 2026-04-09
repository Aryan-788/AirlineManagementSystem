namespace BaggageService.DTOs;

public class AddBaggageDto
{
    public int BookingId { get; set; }
    public decimal Weight { get; set; }
    public string PassengerName { get; set; } = "";
    public string FlightNumber { get; set; } = "";
}

public class UpdateBaggageStatusDto
{
    public string Status { get; set; } = "";
}

public class BaggageDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public decimal Weight { get; set; }
    public string PassengerName { get; set; } = "";
    public string FlightNumber { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsDelivered { get; set; }
    public string TrackingNumber { get; set; } = "";
}

public class BaggageSummaryDto
{
    public int TotalBags { get; set; }
    public int DeliveredCount { get; set; }
    public int InTransitCount { get; set; }
    public int CheckedCount { get; set; }
    public decimal TotalWeight { get; set; }
}
