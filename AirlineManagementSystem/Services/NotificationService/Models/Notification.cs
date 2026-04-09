using Shared.Models;

namespace NotificationService.Models;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public string Email { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public string NotificationType { get; set; } = "";
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public bool IsRead { get; set; }
}
