using System.Globalization;
using System.Text;
using FlagsNotify.Application.DTO;
using FlagsNotify.Domain;
using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.Enuns;
using FlagsNotify.Domain.Interfaces;

namespace FlagsNotify.Application.Notifications;

public sealed class NotificationInboxService
{
    private readonly INotificationRepository _notifications;
    private readonly TimeProvider _timeProvider;

    public NotificationInboxService(
        INotificationRepository notifications,
        TimeProvider timeProvider)
    {
        _notifications = notifications;
        _timeProvider = timeProvider;
    }

    public async Task<NotificationPageDto> ListAsync(
        string recipientUserId,
        string? cursor,
        int size,
        CancellationToken cancellationToken)
    {
        if (size is < 1 or > 10)
        {
            throw new DomainValidationException("O tamanho da página deve estar entre 1 e 10.");
        }

        NotificationCursor? decodedCursor = DecodeCursor(cursor);
        IReadOnlyList<Notification> records = await _notifications.ListAsync(
            recipientUserId,
            decodedCursor,
            size + 1,
            cancellationToken);

        bool hasNextPage = records.Count > size;
        Notification[] pageRecords = records.Take(size).ToArray();
        string? nextCursor = hasNextPage && pageRecords.Length > 0
            ? EncodeCursor(pageRecords[^1])
            : null;

        return new NotificationPageDto(
            pageRecords.Select(Map).ToArray(),
            nextCursor);
    }

    public async Task<UnreadNotificationCountDto> GetUnreadCountAsync(
        string recipientUserId,
        CancellationToken cancellationToken) =>
        new(await _notifications.CountUnreadAsync(recipientUserId, cancellationToken));

    public Task<bool> MarkAsReadAsync(
        Guid notificationId,
        string recipientUserId,
        CancellationToken cancellationToken) =>
        _notifications.MarkAsReadAsync(
            notificationId,
            recipientUserId,
            _timeProvider.GetUtcNow(),
            cancellationToken);

    public async Task<MarkAllNotificationsReadDto> MarkAllAsReadAsync(
        string recipientUserId,
        CancellationToken cancellationToken) =>
        new(await _notifications.MarkAllAsReadAsync(
            recipientUserId,
            _timeProvider.GetUtcNow(),
            cancellationToken));

    public Task<bool> DeleteAsync(
        Guid notificationId,
        string recipientUserId,
        CancellationToken cancellationToken) =>
        _notifications.DeleteAsync(notificationId, recipientUserId, cancellationToken);

    private static NotificationItemDto Map(Notification notification) =>
        new(
            notification.Id,
            notification.EventType switch
            {
                EventTypes.ReceiveDm => "message",
                EventTypes.HandleLike => "success",
                _ => "info"
            },
            notification.Title,
            notification.Body,
            notification.SenderUserId,
            notification.SenderUserName,
            notification.OccurredAt,
            notification.IsRead,
            notification.Route);

    private static string EncodeCursor(Notification notification)
    {
        string value = string.Create(
            CultureInfo.InvariantCulture,
            $"{notification.OccurredAt.UtcDateTime.Ticks}|{notification.Id:N}");
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static NotificationCursor? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            string normalized = cursor.Trim().Replace('-', '+').Replace('_', '/');
            normalized = normalized.PadRight(normalized.Length + ((4 - normalized.Length % 4) % 4), '=');
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(normalized));
            string[] parts = decoded.Split('|', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2 ||
                !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out long ticks) ||
                !Guid.TryParseExact(parts[1], "N", out Guid id))
            {
                throw new FormatException();
            }

            return new NotificationCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id);
        }
        catch (Exception exception) when (
            exception is FormatException or ArgumentException)
        {
            throw new DomainValidationException("O cursor informado é inválido.");
        }
    }
}
