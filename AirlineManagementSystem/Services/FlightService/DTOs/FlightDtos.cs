namespace FlightService.DTOs;

public class CreateFlightDto
{
    public string FlightNumber { get; set; } = "";
    public string Source { get; set; } = "";
    public string Destination { get; set; } = "";
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string Aircraft { get; set; } = "";
    public int TotalSeats { get; set; }
    public int EconomySeats { get; set; }
    public int BusinessSeats { get; set; }
    public int FirstSeats { get; set; }
    public decimal EconomyPrice { get; set; }
    public decimal BusinessPrice { get; set; }
    public decimal FirstClassPrice { get; set; }
}

public class UpdateFlightDto
{
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public string? Gate { get; set; }
    public string? Aircraft { get; set; }
    public string? CrewAssignment { get; set; }
}

public class FlightDto
{
    public int Id { get; set; }
    public string FlightNumber { get; set; } = "";
    public string Source { get; set; } = "";
    public string Destination { get; set; } = "";
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string Gate { get; set; } = "";
    public string Aircraft { get; set; } = "";
    public string Status { get; set; } = "";
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int EconomySeats { get; set; }
    public int BusinessSeats { get; set; }
    public int FirstSeats { get; set; }
    public decimal EconomyPrice { get; set; }
    public decimal BusinessPrice { get; set; }
    public decimal FirstClassPrice { get; set; }
}

public class SearchFlightDto
{
    public string Source { get; set; } = "";
    public string Destination { get; set; } = "";
    public DateTime DepartureDate { get; set; }
}

// --- Schedule DTOs ---

public class CreateScheduleDto
{
    public int FlightId { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string Gate { get; set; } = "";
    public int EconomySeats { get; set; }
    public int BusinessSeats { get; set; }
    public int FirstSeats { get; set; }
    public decimal EconomyPrice { get; set; }
    public decimal BusinessPrice { get; set; }
    public decimal FirstClassPrice { get; set; }
}

public class UpdateScheduleDto
{
    public DateTime? DepartureTime { get; set; }
    public DateTime? ArrivalTime { get; set; }
    public string? Gate { get; set; }
    public string? Status { get; set; }
    public decimal? EconomyPrice { get; set; }
    public decimal? BusinessPrice { get; set; }
    public decimal? FirstClassPrice { get; set; }
    public int? EconomySeats { get; set; }
    public int? BusinessSeats { get; set; }
    public int? FirstSeats { get; set; }
}

public class FlightScheduleDto
{
    public int Id { get; set; }
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = "";
    public string Source { get; set; } = "";
    public string Destination { get; set; } = "";
    public string Aircraft { get; set; } = "";
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public string Gate { get; set; } = "";
    public string Status { get; set; } = "";
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public int EconomySeats { get; set; }
    public int BusinessSeats { get; set; }
    public int FirstSeats { get; set; }
    public decimal EconomyPrice { get; set; }
    public decimal BusinessPrice { get; set; }
    public decimal FirstClassPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}
