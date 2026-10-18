using FlagsNotify.Domain.Enuns;

namespace FlagsNotify.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; private set; }
    public string EventId { get; private set; } = string.Empty;
    public string RecipientUserId { get; private set; } = string.Empty;
    public string? SenderUserId { get; private set; }
    public string? SenderUserName { get; private set; }
    public EventTypes EventType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Route { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public DateTimeOffset? DeliveryCompletedAt { get; private set; }

    public bool IsRead => ReadAt.HasValue;

    private Notification() { }

    private Notification(
        Guid id,
        string eventId,
        string recipientUserId,
        string? senderUserId,
        string? senderUserName,
        EventTypes eventType,
        string title,
        string body,
        string? route,
        DateTimeOffset occurredAt)
    {
        Id = id;
        EventId = eventId;
        RecipientUserId = recipientUserId;
        SenderUserId = senderUserId;
        SenderUserName = senderUserName;
        EventType = eventType;
        Title = title;
        Body = body;
        Route = route;
        OccurredAt = occurredAt;
    }

    public static Notification Create(
        string eventId,
        string? recipientUserId,
        string? senderUserId,
        string? senderUserName,
        EventTypes eventType,
        string title,
        string body,
        string? route,
        DateTimeOffset occurredAt)
    {
        string normalizedEventId = Required(eventId, 200, "eventId");
        string normalizedRecipientUserId = Required(
            recipientUserId,
            200,
            "identificador do destinatário");
        string normalizedTitle = Required(title, 200, "título");
        string normalizedBody = Required(body, 4_000, "corpo");
        string? normalizedSenderId = Optional(senderUserId, 200, "identificador do remetente");
        string? normalizedSenderName = Optional(senderUserName, 200, "nome do remetente");
        string? normalizedRoute = Optional(route, 500, "rota");

        if (normalizedRoute is not null && !normalizedRoute.StartsWith('/'))
        {
            throw new DomainValidationException("A rota da notificação deve ser interna e começar com '/'.");
        }

        return new Notification(
            Guid.NewGuid(),
            normalizedEventId,
            normalizedRecipientUserId,
            normalizedSenderId,
            normalizedSenderName,
            eventType,
            normalizedTitle,
            normalizedBody,
            normalizedRoute,
            occurredAt);
    }

    public void MarkAsRead(DateTimeOffset readAt)
    {
        ReadAt ??= readAt;
    }

    public void MarkDeliveryCompleted(DateTimeOffset completedAt)
    {
        DeliveryCompletedAt ??= completedAt;
    }

    private static string Required(string? value, int maximumLength, string fieldName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 || normalized.Length > maximumLength)
        {
            throw new DomainValidationException(
                $"O campo {fieldName} deve ter entre 1 e {maximumLength} caracteres.");
        }

        return normalized;
    }

    private static string? Optional(string? value, int maximumLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new DomainValidationException(
                $"O campo {fieldName} deve ter no máximo {maximumLength} caracteres.");
        }

        return normalized;
    }
}
