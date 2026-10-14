using System.Text.Json;
using FlagsNotify.Application.Notifications;
using FlagsNotify.Application.Notifications.Commands;
using FlagsNotify.Domain;

namespace FlagsNotify.Infrastructure.Kafka;

public sealed class InteractionEventDeserializer
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly TimeProvider _timeProvider;

    public InteractionEventDeserializer(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public DispatchNotificationCommand Deserialize(string payload)
    {
        InteractionEventEnvelope? interactionEvent =
            JsonSerializer.Deserialize<InteractionEventEnvelope>(payload, JsonOptions);

        if (interactionEvent is null)
        {
            throw new JsonException("O evento Kafka não contém um objeto JSON.");
        }

        string eventId = Required(interactionEvent.EventId, "eventId");
        // string title = Required(interactionEvent.Title, "title");
        // string body = Required(interactionEvent.Body, "body");

        if (string.IsNullOrWhiteSpace(interactionEvent.UserId))
        {
            throw new DomainValidationException(
                "O evento deve conter userId ou expoPushToken.");
        }
        
        // if (string.IsNullOrWhiteSpace(interactionEvent.UserId) &&
        //     string.IsNullOrWhiteSpace(interactionEvent.ExpoPushToken))
        // {
        //     throw new DomainValidationException(
        //         "O evento deve conter userId ou expoPushToken.");
        // }

        var data = interactionEvent.Data?.ToDictionary(
            pair => pair.Key,
            pair => (object?)pair.Value) ??
            new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(interactionEvent.EventType))
        {
            data["eventType"] = interactionEvent.EventType;
        }

        return new DispatchNotificationCommand(
            eventId,
            interactionEvent.UserId,
            ExpoPushToken: "",
            Title: "Opa",
            Body: "dsaa",
            interactionEvent.OccurredOn ?? _timeProvider.GetUtcNow(),
            data);
    }

    private static string Required(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException(
                $"O evento Kafka deve conter a propriedade {propertyName}.");
        }

        return value;
    }

    private sealed record InteractionEventEnvelope(
        string? EventId,
        string? EventType,
        DateTimeOffset? OccurredOn,
        string? UserId,
        // string? ExpoPushToken,
        // string? Title,
        // string? Body,
        Dictionary<string, JsonElement>? Data
        );
}
