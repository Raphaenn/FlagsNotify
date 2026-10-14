using FlagsNotify.Api.Endpoints;

namespace FlagsNotify.Api.Middlewares;

public static class EndpointExtensions
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapNotificationEndpoints();
        app.MapDeviceEndpoints();

        return app;
    }
}