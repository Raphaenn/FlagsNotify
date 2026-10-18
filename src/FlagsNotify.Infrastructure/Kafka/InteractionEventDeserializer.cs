using System.Text.Json;
using FlagsNotify.Application.Notifications;
using FlagsNotify.Application.Notifications.Commands;
using FlagsNotify.Domain;
using FlagsNotify.Domain.Enuns;

namespace FlagsNotify.Infrastructure.Kafka;

public sealed class InteractionEventDeserializer
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };


    public static DispatchNotificationCommand Deserialize(string payload)
    {
        InteractionEventEnvelope? interactionEvent =
            JsonSerializer.Deserialize<InteractionEventEnvelope>(payload, JsonOptions);

        if (interactionEvent is null)
        {
            throw new JsonException("O evento Kafka não contém um objeto JSON.");
        }

        string eventId = Required(interactionEvent.EventId, "eventId");
        string eventType = Required(interactionEvent.EventType, "eventType");
        
        if (!Enum.TryParse<EventTypes>(eventType, true, out var types) ||
            !Enum.IsDefined(types))
        {
            throw new JsonException($"Invalid event type: {eventType}");
        }

        if (string.IsNullOrWhiteSpace(interactionEvent.RecipientId))
        {
            throw new DomainValidationException(
                "O evento deve conter RecipientId.");
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
            interactionEvent.RecipientId,
            types,
            interactionEvent.SenderId,
            interactionEvent.SenderName,
            Title: interactionEvent.Title,
            Body: interactionEvent.Body,
            interactionEvent.OccurredAt,
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
        string EventId,
        string RecipientId,
        string EventType,
        string? SenderId,
        string? SenderName,
        string? Title,
        string? Body,
        DateTimeOffset OccurredAt,
        Dictionary<string, JsonElement>? Data
        );
}
