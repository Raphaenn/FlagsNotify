using FlagsNotify.Application.Notifications;
using FlagsNotify.Application.Notifications.Commands;
using FlagsNotify.Infrastructure.Kafka;

namespace FlagsNotify.Infrastructure.Tests;

public sealed class InteractionEventDeserializerTests
{
    [Fact]
    public void Deserialize_ValidEvent_MapsCommandAndData()
    {
        var deserializer = new InteractionEventDeserializer(TimeProvider.System);
        const string Json = """
            {
              "eventId": "interaction-1",
              "eventType": "interaction.created",
              "occurredAt": "2026-07-30T10:00:00Z",
              "userId": "user-1",
              "title": "Nova interação",
              "body": "Você recebeu uma mensagem.",
              "data": { "interactionId": "abc" }
            }
            """;

        DispatchNotificationCommand command = deserializer.Deserialize(Json);

        Assert.Equal("interaction-1", command.EventId);
        Assert.Equal("user-1", command.UserId);
        Assert.Equal("interaction.created", command.Data["eventType"]);
        Assert.True(command.Data.ContainsKey("interactionId"));
    }
}
