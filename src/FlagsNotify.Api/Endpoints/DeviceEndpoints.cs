using FlagsNotify.Application.Devices.Commands;
using FlagsNotify.Application.DTO;
using FlagsNotify.Application.Notifications;

namespace FlagsNotify.Api.Endpoints;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints
            .MapGroup("/api/v1/devices")
            .WithTags("Devices");

        group.MapPost(
                "/",
                async (
                    RegisterDeviceRequest request,
                    RegisterDeviceCommandHandler handler,
                    CancellationToken cancellationToken) =>
                {
                    RegisterDeviceResult result = await handler.HandleAsync(
                        new RegisterDeviceCommand(
                            request.UserId,
                            request.ExpoPushToken),
                        cancellationToken);

                    return Results.Created(
                        $"/api/v1/devices/{result.Id}",
                        result);
                })
            .WithName("RegisterDevice")
            .WithSummary("Registra ou reativa um ExpoPushToken para um usuário.")
            .Produces<RegisterDeviceResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }
}

public sealed record RegisterDeviceRequest(string UserId, string ExpoPushToken);
