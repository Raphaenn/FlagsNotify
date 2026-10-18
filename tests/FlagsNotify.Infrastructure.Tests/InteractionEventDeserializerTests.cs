using FlagsNotify.Application.Notifications.Commands;
using FlagsNotify.Domain.Enuns;
using FlagsNotify.Infrastructure.Kafka;

namespace FlagsNotify.Infrastructure.Tests;

public sealed class InteractionEventDeserializerTests
{
    [Fact]
    public void Deserialize_ValidEvent_MapsCommandAndData()
    {
        const string Json = """
            {
              "eventId": "interaction-1",
              "eventType": "HandleLike",
              "occurredAt": "2026-07-30T10:00:00Z",
              "recipientId": "user-1",
              "senderId": "user-2",
              "senderName": "Maria",
              "title": "Nova interação",
              "body": "Você recebeu uma mensagem.",
              "data": { "interactionId": "abc" }
            }
            """;

        DispatchNotificationCommand command = InteractionEventDeserializer.Deserialize(Json);

        Assert.Equal("interaction-1", command.EventId);
        Assert.Equal("user-1", command.RecipientId);
        Assert.Equal(EventTypes.HandleLike, command.EventType);
        Assert.Equal("HandleLike", command.Data["eventType"]?.ToString());
        Assert.True(command.Data.ContainsKey("interactionId"));
    }
}
