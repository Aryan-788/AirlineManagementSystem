using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class CreatePassengerDto
{
    [Required(ErrorMessage = "Passenger name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Passenger name must be between 2 and 100 characters")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Passenger age is required")]
    [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
    public int Age { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [StringLength(20)]
    public string Gender { get; set; } = "";

    [Required(ErrorMessage = "Aadhar card number is required")]
    [RegularExpression(@"^\d{12}$", ErrorMessage = "Aadhar card number must be exactly 12 digits")]
    public string AadharCardNo { get; set; } = "";

    public string? SeatNumber { get; set; }
}

public class PassengerResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public string Gender { get; set; } = "";
    public string AadharCardNo { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? SeatNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CancelPassengerDto
{
    [Required(ErrorMessage = "Cancellation reason is required")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 500 characters")]
    public string CancellationReason { get; set; } = "";
}

