using System.Net;
using System.Net.Http.Json;
using FlagsNotify.Application.Abstractions;
using FlagsNotify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlagsNotify.Api.Tests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task HealthLive_ReturnsOk()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RegisterDevice_ThenSendNotification_ReturnsAccepted()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage registration = await client.PostAsJsonAsync(
            "/api/v1/devices",
            new
            {
                userId = "user-1",
                expoPushToken = "ExpoPushToken[integration]"
            });
        HttpResponseMessage notification = await client.PostAsJsonAsync(
            "/api/v1/notifications",
            new
            {
                userId = "user-1",
                title = "Title",
                body = "Body"
            });

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, notification.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("Kafka:Enabled", "false");
                builder.UseSetting(
                    "ConnectionStrings:PostgresRepository",
                    "Host=localhost;Database=flagsnotify_tests;Username=test;Password=test");
                builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<NotificationDbContext>();
                    services.RemoveAll<DbContextOptions<NotificationDbContext>>();
                    services.AddDbContext<NotificationDbContext>(options =>
                        options.UseInMemoryDatabase(
                            $"flagsnotify-tests-{Guid.NewGuid():N}"));

                    services.RemoveAll<IPushNotificationSender>();
                    services.AddSingleton<IPushNotificationSender, FakePushSender>();
                });
            });

    private sealed class FakePushSender : IPushNotificationSender
    {
        public Task<IReadOnlyCollection<PushTicket>> SendAsync(
            IReadOnlyCollection<PushMessage> messages,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<PushTicket> tickets = messages
                .Select(
                    (_, index) => new PushTicket(
                        $"test-{index}",
                        "ok",
                        null,
                        null))
                .ToArray();
            return Task.FromResult(tickets);
        }
    }
}
