using FlagsNotify.Application.Abstractions;
using FlagsNotify.Application.DTO;
using FlagsNotify.Application.Notifications.Commands;
using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.Enuns;
using FlagsNotify.Domain.Interfaces;
using FlagsNotify.Domain.ValueObjects;

namespace FlagsNotify.Application.Tests;

public sealed class NotificationDispatcherTests
{
    [Fact]
    public async Task Dispatch_WithoutRegisteredDevice_StillPersistsInboxNotification()
    {
        var devices = new FakeDeviceRepository();
        var notifications = new FakeNotificationRepository();
        var sender = new FakePushSender();
        var dispatcher = new DispatchNotificationCommandHandler(
            devices,
            notifications,
            sender,
            TimeProvider.System);

        DispatchNotificationResult result = await dispatcher.DispatchAsync(
            CreateCommand(),
            CancellationToken.None);

        Assert.Equal(0, result.RecipientCount);
        Notification stored = Assert.Single(notifications.Items);
        Assert.Equal("user-1", stored.RecipientUserId);
        Assert.Empty(sender.Messages);
    }

    [Fact]
    public async Task Dispatch_WithRegisteredDevices_PersistsAndSendsEveryPush()
    {
        var devices = new FakeDeviceRepository(
            ExpoPushToken.Create("ExpoPushToken[first]"),
            ExpoPushToken.Create("ExpoPushToken[second]"));
        var notifications = new FakeNotificationRepository();
        var sender = new FakePushSender();
        var dispatcher = new DispatchNotificationCommandHandler(
            devices,
            notifications,
            sender,
            TimeProvider.System);

        DispatchNotificationResult result = await dispatcher.DispatchAsync(
            CreateCommand(),
            CancellationToken.None);

        Assert.Equal(2, result.RecipientCount);
        Assert.Single(notifications.Items);
        Assert.Equal(2, sender.Messages.Count);
        Assert.All(sender.Messages, message => Assert.Equal("event-1", message.Data["eventId"]));
    }

    [Fact]
    public async Task Dispatch_SameEventTwice_DoesNotCreateOrSendDuplicate()
    {
        var devices = new FakeDeviceRepository(ExpoPushToken.Create("ExpoPushToken[first]"));
        var notifications = new FakeNotificationRepository();
        var sender = new FakePushSender();
        var dispatcher = new DispatchNotificationCommandHandler(
            devices,
            notifications,
            sender,
            TimeProvider.System);

        DispatchNotificationResult first = await dispatcher.DispatchAsync(
            CreateCommand(),
            CancellationToken.None);
        DispatchNotificationResult second = await dispatcher.DispatchAsync(
            CreateCommand(),
            CancellationToken.None);

        Assert.Equal(first.NotificationId, second.NotificationId);
        Assert.Single(notifications.Items);
        Assert.Single(sender.Messages);
    }

    private static DispatchNotificationCommand CreateCommand() =>
        new(
            "event-1",
            "user-1",
            EventTypes.HandleLike,
            "user-2",
            "Maria",
            "New like",
            "Maria liked you.",
            DateTimeOffset.Parse(
                "2026-07-30T10:00:00Z",
                System.Globalization.CultureInfo.InvariantCulture),
            new Dictionary<string, object?>
            {
                ["route"] = "/(tabs)/(home)/home"
            });

    private sealed class FakeDeviceRepository : IDeviceRepository
    {
        private readonly IReadOnlyCollection<ExpoPushToken> _tokens;

        public FakeDeviceRepository(params ExpoPushToken[] tokens)
        {
            _tokens = tokens;
        }

        public Task<DeviceRegistration> RegisterAsync(
            string userId,
            ExpoPushToken token,
            CancellationToken cancellationToken) =>
            Task.FromResult(DeviceRegistration.Register(userId, token, DateTimeOffset.UtcNow));

        public Task<IReadOnlyCollection<ExpoPushToken>> GetActiveTokensAsync(
            string userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_tokens);

        public Task DeactivateAsync(
            ExpoPushToken token,
            CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public List<Notification> Items { get; } = [];

        public Task<Notification?> FindByEventIdAsync(
            string eventId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.SingleOrDefault(item => item.EventId == eventId));

        public Task AddAsync(Notification notification, CancellationToken cancellationToken)
        {
            Items.Add(notification);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Notification notification, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IReadOnlyList<Notification>> ListAsync(
            string recipientUserId,
            NotificationCursor? cursor,
            int take,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> CountUnreadAsync(
            string recipientUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> MarkAsReadAsync(
            Guid notificationId,
            string recipientUserId,
            DateTimeOffset readAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> MarkAllAsReadAsync(
            string recipientUserId,
            DateTimeOffset readAt,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> DeleteAsync(
            Guid notificationId,
            string recipientUserId,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakePushSender : IPushNotificationSender
    {
        public List<PushMessage> Messages { get; } = [];

        public Task<IReadOnlyCollection<PushTicket>> SendAsync(
            IReadOnlyCollection<PushMessage> messages,
            CancellationToken cancellationToken)
        {
            Messages.AddRange(messages);
            IReadOnlyCollection<PushTicket> tickets = messages
                .Select((_, index) => new PushTicket($"ticket-{index}", "ok", null, null))
                .ToArray();
            return Task.FromResult(tickets);
        }
    }
}
