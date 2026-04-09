namespace AdminService.DTOs;

public class DashboardDto
{
    public int TotalBookings { get; set; }
    public decimal TotalRevenue { get; set; }
    public int ActiveFlights { get; set; }
    public int TotalUsers { get; set; }
}

public class BookingReportDto
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class RevenueReportDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public int BookingCount { get; set; }
}

public class DealerCommissionReportDto
{
    public int DealerId { get; set; }
    public string DealerName { get; set; } = "";
    public int TotalBookings { get; set; }
    public decimal TotalCommission { get; set; }
}

public class RefundReportDto
{
    public int BookingId { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTime RefundDate { get; set; }
}
