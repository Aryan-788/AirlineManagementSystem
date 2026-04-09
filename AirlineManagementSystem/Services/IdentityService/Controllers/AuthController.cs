using IdentityService.DTOs;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register User
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns message</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            await _authService.RegisterAsync(dto);
            return Ok(new { message = "Registration semi-complete. Check your email for the verification OTP." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Verify Registration
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns message</returns>
    [HttpPost("register-verify")]
    public async Task<IActionResult> VerifyRegistration([FromBody] VerifyRegistrationDto dto)
    {
        try
        {
            var result = await _authService.VerifyRegistrationAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Resend Verification
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns message</returns>
    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification([FromBody] ForgotPasswordDto dto) // Reusing the simple Email holder DTO
    {
        try
        {
            await _authService.ResendVerificationAsync(dto.Email);
            return Ok(new { message = "If the account exists and is unverified, a new OTP has been sent." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Login
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns token and user details</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    /// <summary>
    /// OAuth Login
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns token and user details</returns>
    [HttpPost("oauth-login")]
    public async Task<IActionResult> OAuthLogin([FromBody] OAuthLoginDto dto)
    {
        try
        {
            var result = await _authService.OAuthLoginAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get User by ID
    /// </summary>
    /// <param name="userId"></param>
    /// <returns>returns user details</returns>
    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUser(int userId)
    {
        var user = await _authService.GetUserAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        return Ok(user);
    }

    /// <summary>
    /// Update Profile
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="dto"></param>
    /// <returns>returns user details</returns>
    [HttpPut("user/{userId}/profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile(int userId, [FromBody] UpdateProfileDto dto)
    {
        try
        {
            // Optional: Ensure the authenticated user is updating their own profile
            var claimsUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (claimsUserId != userId.ToString())
            {
                return Forbid();
            }

            var result = await _authService.UpdateProfileAsync(userId, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Forgot Password
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns message</returns>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            await _authService.ForgotPasswordAsync(dto);
            return Ok(new { message = "If the email is registered, a reset link will be sent." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reset Password
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>returns message</returns>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            await _authService.ResetPasswordAsync(dto);
            return Ok(new { message = "Password has been reset successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
