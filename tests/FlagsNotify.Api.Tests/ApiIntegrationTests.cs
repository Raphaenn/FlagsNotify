using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FlagsNotify.Application.DTO;
using FlagsNotify.Application.Abstractions;
using FlagsNotify.Domain.Entities;
using FlagsNotify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace FlagsNotify.Api.Tests;

public sealed class ApiIntegrationTests
{
    private const string JwtSigningKey = "flags-notify-integration-tests-key-2026";
    private const string JwtIssuer = "flags-notify-tests";
    private const string JwtAudience = "flags-notify-app-tests";

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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateAccessToken("user-1"));

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
                eventId = "event-1",
                eventType = "HandleLike",
                recipientUserId = "user-1",
                senderUserId = "user-2",
                senderUserName = "Maria",
                title = "Title",
                body = "Body",
                data = new { route = "/(tabs)/(home)/home" }
            });

        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, notification.StatusCode);

        using (IServiceScope scope = factory.Services.CreateScope())
        {
            Notification stored = Assert.Single(
                await scope.ServiceProvider
                    .GetRequiredService<NotificationDbContext>()
                    .Notifications
                    .AsNoTracking()
                    .ToArrayAsync());
            Assert.Equal("user-1", stored.RecipientUserId);
        }

        NotificationPageDto? page = await client.GetFromJsonAsync<NotificationPageDto>(
            "/api/v1/notifications?size=20");
        NotificationItemDto item = Assert.Single(Assert.IsType<NotificationPageDto>(page).Items);
        Assert.Equal("Title", item.Title);
        Assert.Equal("success", item.Type);
        Assert.False(item.Read);

        UnreadNotificationCountDto? unread = await client.GetFromJsonAsync<UnreadNotificationCountDto>(
            "/api/v1/notifications/unread-count");
        Assert.Equal(1, Assert.IsType<UnreadNotificationCountDto>(unread).Count);

        HttpResponseMessage marked = await client.PatchAsync(
            $"/api/v1/notifications/{item.Id}/read",
            null);
        Assert.Equal(HttpStatusCode.NoContent, marked.StatusCode);

        unread = await client.GetFromJsonAsync<UnreadNotificationCountDto>(
            "/api/v1/notifications/unread-count");
        Assert.Equal(0, Assert.IsType<UnreadNotificationCountDto>(unread).Count);

        HttpResponseMessage deleted = await client.DeleteAsync(
            $"/api/v1/notifications/{item.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task NotificationInbox_WithoutBearerToken_ReturnsUnauthorized()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NotificationInbox_PaginatesAndMarksAllAsRead()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateAccessToken("user-1"));

        await SendNotificationAsync(client, "event-older", "2026-10-18T10:00:00Z");
        await SendNotificationAsync(client, "event-newer", "2026-10-18T11:00:00Z");

        NotificationPageDto firstPage = Assert.IsType<NotificationPageDto>(
            await client.GetFromJsonAsync<NotificationPageDto>(
                "/api/v1/notifications?size=1"));
        Assert.Single(firstPage.Items);
        Assert.NotNull(firstPage.NextCursor);

        NotificationPageDto secondPage = Assert.IsType<NotificationPageDto>(
            await client.GetFromJsonAsync<NotificationPageDto>(
                $"/api/v1/notifications?size=1&cursor={Uri.EscapeDataString(firstPage.NextCursor!)}"));
        Assert.Single(secondPage.Items);
        Assert.NotEqual(firstPage.Items[0].Id, secondPage.Items[0].Id);

        HttpResponseMessage markAll = await client.PatchAsync(
            "/api/v1/notifications/read-all",
            null);
        Assert.Equal(HttpStatusCode.OK, markAll.StatusCode);

        UnreadNotificationCountDto? unread = await client.GetFromJsonAsync<UnreadNotificationCountDto>(
            "/api/v1/notifications/unread-count");
        Assert.Equal(0, Assert.IsType<UnreadNotificationCountDto>(unread).Count);
    }

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("Kafka:Enabled", "false");
                builder.UseSetting("Jwt:SigningKey", JwtSigningKey);
                builder.UseSetting("Jwt:Issuer", JwtIssuer);
                builder.UseSetting("Jwt:Audience", JwtAudience);
                builder.UseSetting(
                    "ConnectionStrings:PostgresRepository",
                    "Host=localhost;Database=flagsnotify_tests;Username=test;Password=test");
                builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
                builder.ConfigureTestServices(services =>
                {
                    string databaseName = $"flagsnotify-tests-{Guid.NewGuid():N}";
                    services.RemoveAll<NotificationDbContext>();
                    services.RemoveAll<DbContextOptions<NotificationDbContext>>();
                    services.AddDbContext<NotificationDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));

                    services.RemoveAll<IPushNotificationSender>();
                    services.AddSingleton<IPushNotificationSender, FakePushSender>();
                });
            });

    private static string CreateAccessToken(string userId)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            JwtIssuer,
            JwtAudience,
            [new Claim(ClaimTypes.NameIdentifier, userId)],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task SendNotificationAsync(
        HttpClient client,
        string eventId,
        string occurredAt)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/notifications",
            new
            {
                eventId,
                eventType = "HandleLike",
                recipientUserId = "user-1",
                senderUserId = "user-2",
                senderUserName = "Maria",
                title = "Title",
                body = "Body",
                occurredAt,
                data = new { route = "/(tabs)/(home)/home" }
            });
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

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
