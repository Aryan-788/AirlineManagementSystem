using NotificationService.Models;
using Microsoft.EntityFrameworkCore;
namespace NotificationService.Repositories;

public interface INotificationRepository
{
    Task<Notification> AddAsync(Notification notification);
    Task UpdateAsync(Notification notification);
    Task<Notification?> GetByIdAsync(int id);
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
}

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationService.Data.NotificationDbContext _context;

    public NotificationRepository(NotificationService.Data.NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> AddAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task UpdateAsync(Notification notification)
    {
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications.FindAsync(id);
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(int userId)
    {
        return await _context.Notifications.Where(n => n.UserId == userId).ToListAsync();
    }
}
