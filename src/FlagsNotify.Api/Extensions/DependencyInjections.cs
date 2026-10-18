using FlagsNotify.Application.Abstractions;
using FlagsNotify.Application.Devices.Commands;
using FlagsNotify.Application.Notifications;
using FlagsNotify.Application.Notifications.Commands;
using FlagsNotify.Domain.Interfaces;
using FlagsNotify.Infrastructure.Persistence;
using FlagsNotify.Infrastructure.Push;

namespace FlagsNotify.Api.Extensions;

public static class DependencyInjections
{
    public static IServiceCollection RegisterServices(this IServiceCollection service, IConfiguration configuration)
    {
        service.AddScoped<RegisterDeviceCommandHandler>();
        // service.AddScoped<IDeviceRepository, DeviceRegistrationRepository>();
        // service.AddScoped<IPushNotificationSender, PushNotificationSender>();
        service.AddScoped<DispatchNotificationCommandHandler>();
        service.AddScoped<NotificationInboxService>();

        return service;
    }
}
