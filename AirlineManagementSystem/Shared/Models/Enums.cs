namespace Shared.Models;

public class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum UserRole
{
    Admin,
    Passenger,
    Dealer,
    GroundStaff
}

public enum BookingStatus
{
    Pending,
    Confirmed,
    CheckedIn,
    Completed,
    Cancelled
}

public enum PaymentStatus
{
    Pending,
    Success,
    Failed,
    Refunded
}

public enum FlightStatus
{
    Scheduled,
    Boarding,
    Departed,
    InFlight,
    Landed,
    Delayed,
    Cancelled,
    Completed
}

public enum BaggageStatus
{
    Checked,
    Loaded,
    InTransit,
    Delivered,
    Lost
}

public enum SeatClass
{
    Economy,
    Business,
    First
}
