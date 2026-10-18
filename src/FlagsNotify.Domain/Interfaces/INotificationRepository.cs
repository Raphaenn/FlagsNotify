using FlagsNotify.Domain.Entities;

namespace FlagsNotify.Domain.Interfaces;

public sealed record NotificationCursor(DateTimeOffset OccurredAt, Guid Id);

public interface INotificationRepository
{
    Task<Notification?> FindByEventIdAsync(string eventId, CancellationToken cancellationToken);
    
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    
    Task UpdateAsync(Notification notification, CancellationToken cancellationToken);
    
    Task<IReadOnlyList<Notification>> ListAsync(
        string recipientUserId,
        NotificationCursor? cursor,
        int take,
        CancellationToken cancellationToken);
    
    Task<int> CountUnreadAsync(string recipientUserId, CancellationToken cancellationToken);
    
    Task<bool> MarkAsReadAsync(
        Guid notificationId,
        string recipientUserId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken);
    
    Task<int> MarkAllAsReadAsync(
        string recipientUserId,
        DateTimeOffset readAt,
        CancellationToken cancellationToken);
    
    Task<bool> DeleteAsync(
        Guid notificationId,
        string recipientUserId,
        CancellationToken cancellationToken);
}
