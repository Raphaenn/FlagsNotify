using System.Text.Json;
using FlagsNotify.Application.DTO;
using FlagsNotify.Application.Notifications;
using FlagsNotify.Application.Notifications.Commands;

namespace FlagsNotify.Api.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/v1/notifications")
            .WithTags("Notifications");

        group.MapPost(
                "/",
                async (
                    SendNotificationRequest request,
                    DispatchNotificationCommandHandler dispatcher,
                    TimeProvider timeProvider,
                    CancellationToken cancellationToken) =>
                {
                    var data = request.Data?.ToDictionary(
                        pair => pair.Key,
                        pair => (object?)pair.Value) ??
                        new Dictionary<string, object?>();

                    DispatchNotificationResult result = await dispatcher.DispatchAsync(
                        new DispatchNotificationCommand(
                            request.EventId ?? Guid.NewGuid().ToString("N"),
                            request.UserId,
                            request.ExpoPushToken,
                            request.Title,
                            request.Body,
                            request.OccurredAt ?? timeProvider.GetUtcNow(),
                            data),
                        cancellationToken);

                    return Results.Accepted(value: result);
                })
            .WithName("SendNotification")
            .WithSummary("Envia uma notificação diretamente, sem passar pelo Kafka.")
            .Produces<DispatchNotificationResult>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}
