using NotificationService.DTOs;
using NotificationService.Models;
using NotificationService.Repositories;
using Shared.Events;
using Shared.RabbitMQ;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NotificationService.Services;

public interface INotificationService
{
    Task<NotificationDto> GetNotificationAsync(int id);
    Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId);
    Task HandleBookingCreatedAsync(BookingCreatedEvent @event);
    Task HandlePaymentSuccessAsync(PaymentSuccessEvent @event);
    Task HandlePaymentFailedAsync(PaymentFailedEvent @event);
    Task HandleFlightDelayedAsync(FlightDelayedEvent @event);
    Task HandleCheckInCompletedAsync(CheckInCompletedEvent @event);
    Task HandlePasswordResetRequestedAsync(PasswordResetRequestedEvent @event);
    Task HandleUserRegistrationRequestedAsync(UserRegistrationRequestedEvent @event);
    Task HandleRefundProcessedAsync(RefundProcessedEvent @event);
    Task MarkAsReadAsync(int id);
    Task MarkAllAsReadAsync(int userId);
}

public class NotificationServiceImpl : INotificationService
{
    private readonly INotificationRepository _repository;
    private readonly IEmailService _emailService;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationServiceImpl> _logger;

    public NotificationServiceImpl(
        INotificationRepository repository,
        IEmailService emailService,
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<NotificationServiceImpl> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<NotificationDto> GetNotificationAsync(int id)
    {
        var notification = await _repository.GetByIdAsync(id);
        if (notification == null)
            throw new KeyNotFoundException($"Notification {id} not found");

        return MapToDto(notification);
    }

    public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(int userId)
    {
        var notifications = await _repository.GetByUserIdAsync(userId);
        return notifications.Select(MapToDto);
    }

    private async Task<string> GetUserEmailAsync(int userId)
    {
        if (userId <= 0) return "";

        try
        {
            var identityUrl = _configuration["IdentityServiceUrl"] ?? "http://localhost:5001";
            
            var token = GenerateSystemToken();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{identityUrl}/api/auth/user/{userId}");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<JsonElement>(content);
                if (user.TryGetProperty("email", out var emailProp))
                {
                    return emailProp.GetString() ?? "";
                }
            }
        }
        catch
        {
            // Ignore errors and return empty email
        }
        
        return "";
    }

    private string GenerateSystemToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "999999"),
            new Claim(ClaimTypes.Email, "system@skyledger.com"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task SaveAndSendNotificationAsync(Notification notification)
    {
        if (string.IsNullOrWhiteSpace(notification.Email) && notification.UserId > 0)
        {
            notification.Email = await GetUserEmailAsync(notification.UserId);
        }

        await _repository.AddAsync(notification);

        if (!string.IsNullOrWhiteSpace(notification.Email))
        {
            try 
            {
                await _emailService.SendEmailAsync(notification.Email, notification.Subject, notification.Message);
                
                notification.IsSent = true;
                notification.SentAt = DateTime.UtcNow;
                await _repository.UpdateAsync(notification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", notification.Email);
                throw;
            }
        }
    }

    public async Task HandleBookingCreatedAsync(BookingCreatedEvent @event)
    {
        var notification = new Notification
        {
            UserId = @event.UserId,
            Email = "",
            Subject = "Booking Initiated",
            Message = $"Your booking (ID: {@event.BookingId}) is pending payment. Please complete payment to confirm your seats.",
            NotificationType = "BookingInitiated",
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };

        await SaveAndSendNotificationAsync(notification);
    }

    public async Task HandlePaymentSuccessAsync(PaymentSuccessEvent @event)
    {
        var notification = new Notification
        {
            UserId = @event.UserId,
            Email = "",
            Subject = "Booking Confirmed",
            Message = $"Your payment of ₹{@event.Amount} for booking {@event.BookingId} was successful. Your journey is now confirmed!",
            NotificationType = "BookingConfirmed",
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };

        await SaveAndSendNotificationAsync(notification);
    }

    public async Task HandlePaymentFailedAsync(PaymentFailedEvent @event)
    {
        var notification = new Notification
        {
            UserId = @event.UserId, // Ensure we have the user ID to fetch the email
            Email = "",
            Subject = "Payment Failed",
            Message = $"Payment for booking {@event.BookingId} failed. Reason: {@event.Reason}",
            NotificationType = "PaymentFailed",
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };

        await SaveAndSendNotificationAsync(notification);
    }

    public async Task HandleFlightDelayedAsync(FlightDelayedEvent @event)
    {
        // For flight delays, we would normally notify all users on the flight. 
        // For now, this is a placeholder behavior as per current implementation structure.
        var notification = new Notification
        {
            UserId = 0,
            Email = "",
            Subject = "Flight Delay Notification",
            Message = $"Flight {@event.FlightNumber} has been delayed. New departure time: {@event.NewDepartureTime:yyyy-MM-dd HH:mm:ss}",
            NotificationType = "FlightDelayed",
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };

        await SaveAndSendNotificationAsync(notification);
    }

    public async Task HandleCheckInCompletedAsync(CheckInCompletedEvent @event)
    {
        var notification = new Notification
        {
            UserId = @event.UserId,
            Email = "",
            Subject = "Check-in Successful",
            Message = $"You have successfully checked in for flight. Boarding Pass: <br/><br/><strong>{@event.BoardingPass}</strong>",
            NotificationType = "CheckInConfirmation",
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };

        await SaveAndSendNotificationAsync(notification);
    }

    public async Task HandlePasswordResetRequestedAsync(PasswordResetRequestedEvent @event)
    {
        var notification = new Notification
        {
            UserId = @event.UserId,
            Email = @event.Email,
            Subject = "Password Reset Request",
            Message = $"You requested a password reset. Your OTP is: <br/><br/><strong style='font-size:24px;color:#1d4ed8;'>{@event.ResetToken}</strong><br/><br/>This code will expire in 15 minutes.",
            NotificationType = "PasswordReset",
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };

        await SaveAndSendNotificationAsync(notification);
    }

    public async Task HandleUserRegistrationRequestedAsync(UserRegistrationRequestedEvent @event)
    {
        var notification = new Notification
        {
            UserId = @event.UserId,
            Email = @event.Email,
            Subject = "Verify Your Account",
            Message = $"Welcome to SkyLedger Airlines! To complete your registration, please enter the following OTP: <br/><br/><strong style='font-size:24px;color:#1d4ed8;'>{@event.VerificationToken}</strong><br/><br/>This code will expire in 15 minutes. If you did not create this account, you can safely ignore this email.",
            NotificationType = "UserVerification",
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };

        await SaveAndSendNotificationAsync(notification);
    }

    public async Task HandleRefundProcessedAsync(RefundProcessedEvent @event)
    {
        string message;
        if (@event.RefundAmount > 0)
        {
            message = $"Your booking cancellation has been processed. <br/><br/>" +
                      $"<strong>Refund Amount: ₹{@event.RefundAmount:F2}</strong><br/>" +
                      $"Refund Percentage Applied: {@event.RefundPercentage}%<br/><br/>" +
                      $"Your refund is initiated and will be deposited within <strong>5 - 6 working days</strong> to your original payment method.";
        }
        else
        {
            message = $"Your booking/passenger cancellation has been processed. <br/><br/>" +
                      $"Unfortunately, based on our cancellation policy, <strong>no refund is applicable</strong> as the departure was within the non-refundable window.<br/><br/>" +
                      $"We hope to serve you again on SkyLedger Airlines.";
        }

        var notification = new Notification
        {
            UserId = @event.UserId,
            Email = "",
            Subject = @event.RefundAmount > 0 ? "Refund Initiated – 5 to 6 Working Days" : "Booking Cancelled – No Refund Applicable",
            Message = message,
            NotificationType = "RefundProcessed",
            IsSent = false,
            CreatedAt = DateTime.UtcNow
        };

        await SaveAndSendNotificationAsync(notification);
    }

    public async Task MarkAsReadAsync(int id)
    {
        var notification = await _repository.GetByIdAsync(id);
        if (notification != null)
        {
            notification.IsRead = true;
            await _repository.UpdateAsync(notification);
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var notifications = await _repository.GetByUserIdAsync(userId);
        foreach (var notification in notifications.Where(n => !n.IsRead))
        {
            notification.IsRead = true;
            await _repository.UpdateAsync(notification);
        }
    }

    private NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Email = notification.Email,
            Subject = notification.Subject,
            Message = notification.Message,
            NotificationType = notification.NotificationType,
            IsSent = notification.IsSent,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };
    }
}
