using Shared.Models;

namespace IdentityService.Models;

public class User : BaseEntity
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    // Password Reset fields
    public string? ResetToken { get; set; }
    public DateTime? ResetTokenExpiry { get; set; }

    // Email Verification fields
    public bool IsEmailVerified { get; set; } = false;
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenExpiry { get; set; }
}
