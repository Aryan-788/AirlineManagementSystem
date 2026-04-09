namespace NotificationService.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public string NotificationType { get; set; } = "";
    public bool IsSent { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
