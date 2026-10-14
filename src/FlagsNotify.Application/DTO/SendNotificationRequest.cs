using System.Text.Json;

namespace FlagsNotify.Application.DTO;

public sealed record SendNotificationRequest(
    string? EventId,
    string? UserId,
    string? ExpoPushToken,
    string Title,
    string Body,
    DateTimeOffset? OccurredAt,
    Dictionary<string, JsonElement>? Data);