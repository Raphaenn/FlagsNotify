using System.Text.Json;
using System.Security.Claims;
using FlagsNotify.Application.DTO;
using FlagsNotify.Application.Notifications;
using FlagsNotify.Application.Notifications.Commands;
using FlagsNotify.Domain;
using FlagsNotify.Domain.Enuns;

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

                    if (!Enum.TryParse(request.EventType, true, out EventTypes type) ||
                        !Enum.IsDefined(type))
                    {
                        throw new DomainValidationException("O tipo de notificação é inválido.");
                    }
                    
                    DispatchNotificationResult result = await dispatcher.DispatchAsync(
                        new DispatchNotificationCommand(
                            request.EventId,
                            request.RecipientUserId,
                            type,
                            request.SenderUserId,
                            request.SenderUserName,
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

        group.MapGet(
                "/",
                async (
                    ClaimsPrincipal user,
                    NotificationInboxService inbox,
                    string? cursor,
                    int? size,
                    CancellationToken cancellationToken) =>
                {
                    NotificationPageDto page = await inbox.ListAsync(
                        GetUserId(user),
                        cursor,
                        size ?? 20,
                        cancellationToken);
                    return Results.Ok(page);
                })
            .RequireAuthorization()
            .WithName("ListNotifications")
            .Produces<NotificationPageDto>();

        group.MapGet(
                "/unread-count",
                async (
                    ClaimsPrincipal user,
                    NotificationInboxService inbox,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await inbox.GetUnreadCountAsync(
                        GetUserId(user),
                        cancellationToken)))
            .RequireAuthorization()
            .WithName("GetUnreadNotificationCount")
            .Produces<UnreadNotificationCountDto>();

        group.MapPatch(
                "/{notificationId:guid}/read",
                async (
                    Guid notificationId,
                    ClaimsPrincipal user,
                    NotificationInboxService inbox,
                    CancellationToken cancellationToken) =>
                    await inbox.MarkAsReadAsync(
                        notificationId,
                        GetUserId(user),
                        cancellationToken)
                        ? Results.NoContent()
                        : Results.NotFound())
            .RequireAuthorization()
            .WithName("MarkNotificationAsRead")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPatch(
                "/read-all",
                async (
                    ClaimsPrincipal user,
                    NotificationInboxService inbox,
                    CancellationToken cancellationToken) =>
                    Results.Ok(await inbox.MarkAllAsReadAsync(
                        GetUserId(user),
                        cancellationToken)))
            .RequireAuthorization()
            .WithName("MarkAllNotificationsAsRead")
            .Produces<MarkAllNotificationsReadDto>();

        group.MapDelete(
                "/{notificationId:guid}",
                async (
                    Guid notificationId,
                    ClaimsPrincipal user,
                    NotificationInboxService inbox,
                    CancellationToken cancellationToken) =>
                    await inbox.DeleteAsync(
                        notificationId,
                        GetUserId(user),
                        cancellationToken)
                        ? Results.NoContent()
                        : Results.NotFound())
            .RequireAuthorization()
            .WithName("DeleteNotification")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static string GetUserId(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ??
        user.FindFirstValue("sub") ??
        throw new UnauthorizedAccessException("O token não contém o identificador do usuário.");
}
