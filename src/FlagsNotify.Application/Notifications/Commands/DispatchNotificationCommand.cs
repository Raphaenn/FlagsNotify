namespace FlagsNotify.Application.Notifications.Commands;

public sealed record DispatchNotificationCommand(
    string EventId,
    string? UserId,
    string? ExpoPushToken,
    string Title,
    string Body,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, object?> Data);

