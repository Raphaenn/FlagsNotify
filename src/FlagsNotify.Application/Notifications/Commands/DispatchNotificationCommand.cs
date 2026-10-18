using FlagsNotify.Domain.Enuns;

namespace FlagsNotify.Application.Notifications.Commands;

public sealed record DispatchNotificationCommand(
    string EventId,
    string RecipientId,
    EventTypes EventType,
    string? SenderId,
    string? SenderName,
    string? Title,
    string? Body,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, object?> Data);