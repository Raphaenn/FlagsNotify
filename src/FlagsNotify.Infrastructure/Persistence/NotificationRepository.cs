using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FlagsNotify.Infrastructure.Persistence;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _dbContext;

    public NotificationRepository(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Notification?> FindByEventIdAsync(
        string eventId,
        CancellationToken cancellationToken) =>
        _dbContext.Notifications.SingleOrDefaultAsync(
            notification => notification.EventId == eventId,
            cancellationToken);

    public async Task AddAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        _dbContext.Notifications.Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Notification notification,
        CancellationToken cancellationToken)
    {
        _dbContext.Notifications.Update(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> ListAsync(
        string recipientUserId,
        NotificationCursor? cursor,
        int take,
        CancellationToken cancellationToken)
    {
        IQueryable<Notification> query = _dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.RecipientUserId == recipientUserId);

        if (cursor is not null)
        {
            query = query.Where(notification =>
                notification.OccurredAt < cursor.OccurredAt ||
                (notification.OccurredAt == cursor.OccurredAt &&
                    notification.Id.CompareTo(cursor.Id) < 0));
        }

        return await query
            .OrderByDescending(notification => notification.OccurredAt)
            .ThenByDescending(notification => notification.Id)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountUnreadAsync(
        string recipientUserId,
        CancellationToken cancellationToken) =>
        _dbContext.Notifications.CountAsync(
            notification =>
                notification.RecipientUserId == recipientUserId &&
                notification.ReadAt == null,
            cancellationToken);

    public async Task<bool> MarkAsReadAsync(
        Guid notificationId,
        string recipientUserId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken)
    {
        Notification? notification = await _dbContext.Notifications.SingleOrDefaultAsync(
            item => item.Id == notificationId && item.RecipientUserId == recipientUserId,
            cancellationToken);

        if (notification is null)
        {
            return false;
        }

        notification.MarkAsRead(readAt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(
        string recipientUserId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken)
    {
        Notification[] notifications = await _dbContext.Notifications
            .Where(notification =>
                notification.RecipientUserId == recipientUserId &&
                notification.ReadAt == null)
            .ToArrayAsync(cancellationToken);

        foreach (Notification notification in notifications)
        {
            notification.MarkAsRead(readAt);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return notifications.Length;
    }

    public async Task<bool> DeleteAsync(
        Guid notificationId,
        string recipientUserId,
        CancellationToken cancellationToken)
    {
        Notification? notification = await _dbContext.Notifications.SingleOrDefaultAsync(
            item => item.Id == notificationId && item.RecipientUserId == recipientUserId,
            cancellationToken);

        if (notification is null)
        {
            return false;
        }

        _dbContext.Notifications.Remove(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
