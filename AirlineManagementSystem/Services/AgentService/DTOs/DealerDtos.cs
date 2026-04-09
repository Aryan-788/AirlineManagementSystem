namespace AgentService.DTOs;

public class CreateDealerDto
{
    public string DealerName { get; set; } = "";
    public string DealerEmail { get; set; } = "";
    public int AllocatedSeats { get; set; }
    public decimal CommissionRate { get; set; }
}

public class DealerDto
{
    public int Id { get; set; }
    public string DealerName { get; set; } = "";
    public string DealerEmail { get; set; } = "";
    public int AllocatedSeats { get; set; }
    public int UsedSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal CommissionRate { get; set; }
    public bool IsActive { get; set; }
}

public class DealerCommissionReportDto
{
    public int DealerId { get; set; }
    public string DealerName { get; set; } = "";
    public int TotalBookings { get; set; }
    public decimal TotalCommission { get; set; }
}

public class DealerBookingDto
{
    public int Id { get; set; }
    public int DealerId { get; set; }
    public int BookingId { get; set; }
    public int FlightId { get; set; }
    public decimal Commission { get; set; }
}

public class DealerPerformanceDto
{
    public int AllocatedSeats { get; set; }
    public int UsedSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal TotalCommission { get; set; }
    public int TotalBookings { get; set; }
    public decimal CommissionRate { get; set; }
}
