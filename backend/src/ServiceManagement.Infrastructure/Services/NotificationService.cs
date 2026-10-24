using Microsoft.EntityFrameworkCore;
using ServiceManagement.Application.Common.Exceptions;
using ServiceManagement.Application.Notifications.Dtos;
using ServiceManagement.Application.Notifications.Interfaces;
using ServiceManagement.Infrastructure.Persistence;

namespace ServiceManagement.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserNotificationDto>> ListMineAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UserNotifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new UserNotificationDto(
                n.Id,
                n.Title,
                n.Body,
                n.ServiceRequestId,
                n.IsRead,
                n.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<UserNotificationDto> MarkReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var row = await _db.UserNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Notification was not found.");

        if (!row.IsRead)
        {
            row.IsRead = true;
            row.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new UserNotificationDto(
            row.Id,
            row.Title,
            row.Body,
            row.ServiceRequestId,
            row.IsRead,
            row.CreatedAt);
    }

    public async Task<int> MarkAllReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var unread = await _db.UserNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var row in unread)
        {
            row.IsRead = true;
            row.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return unread.Count;
    }
}
