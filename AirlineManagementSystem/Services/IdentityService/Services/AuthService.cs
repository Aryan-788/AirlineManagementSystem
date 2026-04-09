using IdentityService.DTOs;
using IdentityService.Models;
using IdentityService.Repositories;
using Shared.Events;
using Shared.RabbitMQ;
using Shared.Security;
using BCrypt.Net;

namespace IdentityService.Services;

public interface IAuthService
{
    Task RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> VerifyRegistrationAsync(VerifyRegistrationDto dto);
    Task ResendVerificationAsync(string email);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<UserDto?> GetUserAsync(int userId);
    Task<AuthResponseDto> OAuthLoginAsync(OAuthLoginDto dto);
    Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IEventPublisher _eventPublisher;

    public AuthService(IUserRepository userRepository, ITokenService tokenService, IEventPublisher eventPublisher)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _eventPublisher = eventPublisher;
    }

    public async Task RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("Email already exists");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var verificationToken = new Random().Next(100000, 999999).ToString();

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = passwordHash,
            Role = dto.Role,
            CreatedAt = DateTime.UtcNow,
            IsEmailVerified = false,
            VerificationToken = verificationToken,
            VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15)
        };

        await _userRepository.AddAsync(user);

        var registrationEvent = new UserRegistrationRequestedEvent(
            UserId: user.Id,
            Email: user.Email,
            VerificationToken: verificationToken,
            RequestedAt: DateTime.UtcNow
        );

        await _eventPublisher.PublishAsync(registrationEvent);
    }

    public async Task<AuthResponseDto> VerifyRegistrationAsync(VerifyRegistrationDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
            throw new InvalidOperationException("Invalid token or email.");

        if (user.IsEmailVerified)
            throw new InvalidOperationException("Email is already verified.");

        if (user.VerificationToken != dto.Token || user.VerificationTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Token is invalid or has expired.");

        user.IsEmailVerified = true;
        user.VerificationToken = null;
        user.VerificationTokenExpiry = null;

        await _userRepository.UpdateAsync(user);

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            Token = token
        };
    }

    public async Task ResendVerificationAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null || user.IsEmailVerified)
        {
            // Do not throw an error to prevent user enumeration or unnecessary errors
            return;
        }

        // Generate a new 6-digit OTP
        var verificationToken = new Random().Next(100000, 999999).ToString();
        user.VerificationToken = verificationToken;
        user.VerificationTokenExpiry = DateTime.UtcNow.AddMinutes(15);

        await _userRepository.UpdateAsync(user);

        var registrationEvent = new UserRegistrationRequestedEvent(
            UserId: user.Id,
            Email: user.Email,
            VerificationToken: verificationToken,
            RequestedAt: DateTime.UtcNow
        );

        await _eventPublisher.PublishAsync(registrationEvent);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("User is inactive");
            
        if (!user.IsEmailVerified)
            throw new UnauthorizedAccessException("Please verify your email before logging in.");

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            Token = token
        };
    }

    public async Task<UserDto?> GetUserAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<AuthResponseDto> OAuthLoginAsync(OAuthLoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        
        if (user == null)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString() + "OAuth!12A");
            user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = passwordHash,
                Role = Shared.Models.UserRole.Passenger,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            await _userRepository.AddAsync(user);
        }
        else if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User is inactive");
        }

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name,
            Role = user.Role.ToString(),
            Token = token
        };
    }

    public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        user.Name = dto.Name;
        if (!string.IsNullOrWhiteSpace(dto.Email) && user.Email != dto.Email)
        {
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
                throw new InvalidOperationException("Email is already taken");
            
            user.Email = dto.Email;
        }

        await _userRepository.UpdateAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt
        };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
        {
            // Do not throw an error to prevent user enumeration
            return;
        }

        // Generate a 6-digit OTP
        var resetToken = new Random().Next(100000, 999999).ToString();
        user.ResetToken = resetToken;
        user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);

        await _userRepository.UpdateAsync(user);

        // Publish event to NotificationService
        var passwordResetEvent = new PasswordResetRequestedEvent(
            UserId: user.Id,
            Email: user.Email,
            ResetToken: resetToken,
            RequestedAt: DateTime.UtcNow
        );

        await _eventPublisher.PublishAsync(passwordResetEvent);
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email);
        if (user == null || !user.IsActive)
        {
            throw new InvalidOperationException("Invalid token or email.");
        }

        if (user.ResetToken != dto.Token || user.ResetTokenExpiry < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Token is invalid or has expired.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.ResetToken = null;
        user.ResetTokenExpiry = null;

        await _userRepository.UpdateAsync(user);
    }
}
