using NotificationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get Notification by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetNotification(int id)
    {
        try
        {
            var result = await _notificationService.GetNotificationAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get User Notifications
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpGet("user/{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUserNotifications(int userId)
    {
        try
        {
            var result = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Mark Notification as Read
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpPut("mark-as-read/{id}")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Ok();
    }

    /// <summary>
    /// Mark All Notifications as Read
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpPut("mark-all-as-read/{userId}")]
    [Authorize]
    public async Task<IActionResult> MarkAllAsRead(int userId)
    {
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok();
    }
}
