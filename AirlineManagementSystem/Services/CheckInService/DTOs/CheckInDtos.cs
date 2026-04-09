namespace CheckInService.DTOs;

public class OnlineCheckInDto
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public string? SeatNumber { get; set; }
}

public class CheckInDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string SeatNumber { get; set; } = "";
    public string Gate { get; set; } = "";
    public string BoardingPass { get; set; } = "";
    public DateTime CheckInTime { get; set; }
}

public class BoardingPassDto
{
    public string PassengerName { get; set; } = "";
    public string FlightNumber { get; set; } = "";
    public string Gate { get; set; } = "";
    public string SeatNumber { get; set; } = "";
    public string QRCode { get; set; } = "";
    public DateTime DepartureTime { get; set; }
}

public class CheckInSummaryDto
{
    public int TotalCheckIns { get; set; }
    public int TodayCheckIns { get; set; }
}
