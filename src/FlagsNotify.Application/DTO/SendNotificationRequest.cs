using System.Text.Json;

namespace FlagsNotify.Application.DTO;

public sealed record SendNotificationRequest(
    string EventId,
    string EventType,
    string RecipientUserId,
    string SenderUserId,
    string SenderUserName,
    string Title,
    string Body,
    DateTimeOffset? OccurredAt,
    Dictionary<string, JsonElement>? Data);