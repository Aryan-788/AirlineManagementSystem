using Shared.Models;

namespace IdentityService.DTOs;

public class RegisterDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public UserRole Role { get; set; }
}

public class OAuthLoginDto
{
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
}

public class LoginDto
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Token { get; set; } = "";
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileDto
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public class ForgotPasswordDto
{
    public string Email { get; set; } = "";
}

public class ResetPasswordDto
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class VerifyRegistrationDto
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
}
