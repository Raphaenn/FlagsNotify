namespace FlagsNotify.Domain.Entities;

public sealed class Notification
{
    public Guid Id { get; }
    public string EventId { get; }
    public string? RecipientUserId { get; }
    public string Title { get; }
    public string Body { get; }
    public DateTimeOffset OccurredAt { get; }
    
    private Notification(
        Guid id,
        string eventId,
        string? recipientUserId,
        string title,
        string body,
        DateTimeOffset occurredAt)
    {
        Id = id;
        EventId = eventId;
        RecipientUserId = recipientUserId;
        Title = title;
        Body = body;
        OccurredAt = occurredAt;
    }

    public static Notification Create(
        string eventId,
        string? recipientUserId,
        string title,
        string body,
        DateTimeOffset occurredAt)
    {
        string normalizedEventId = Required(eventId, 200, "eventId");
        string normalizedTitle = Required(title, 200, "título");
        string normalizedBody = Required(body, 4_000, "corpo");
        string? normalizedUserId = string.IsNullOrWhiteSpace(recipientUserId)
            ? null
            : recipientUserId.Trim();

        if (normalizedUserId?.Length > 200)
        {
            throw new DomainValidationException("O identificador do usuário deve ter no máximo 200 caracteres.");
        }

        return new Notification(
            Guid.NewGuid(),
            normalizedEventId,
            normalizedUserId,
            normalizedTitle,
            normalizedBody,
            occurredAt);
    }

    private static string Required(string value, int maximumLength, string fieldName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 || normalized.Length > maximumLength)
        {
            throw new DomainValidationException(
                $"O campo {fieldName} deve ter entre 1 e {maximumLength} caracteres.");
        }

        return normalized;
    }
}
