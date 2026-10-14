// using FlagsNotify.Application.Abstractions;
// using FlagsNotify.Application.Notifications;
// using FlagsNotify.Domain.Entities;
// using FlagsNotify.Domain.ValueObjects;
//
// namespace FlagsNotify.Application.Tests;
//
// public sealed class NotificationDispatcherTests
// {
//     [Fact]
//     public async Task Dispatch_WithDirectToken_SendsOnePush()
//     {
//         var repository = new FakeDeviceRepository();
//         var sender = new FakePushSender();
//         var dispatcher = new NotificationDispatcher(repository, sender);
//
//         DispatchNotificationResult result = await dispatcher.DispatchAsync(
//             CreateCommand(expoPushToken: "ExpoPushToken[direct]"),
//             CancellationToken.None);
//
//         Assert.Equal(1, result.RecipientCount);
//         PushMessage sent = Assert.Single(sender.Messages);
//         Assert.Equal("ExpoPushToken[direct]", sent.To);
//         Assert.Equal("event-1", sent.Data["eventId"]);
//     }
//
//     [Fact]
//     public async Task Dispatch_WithUserId_ResolvesEveryActiveDevice()
//     {
//         var repository = new FakeDeviceRepository(
//             ExpoPushToken.Create("ExpoPushToken[first]"),
//             ExpoPushToken.Create("ExpoPushToken[second]"));
//         var sender = new FakePushSender();
//         var dispatcher = new NotificationDispatcher(repository, sender);
//
//         DispatchNotificationResult result = await dispatcher.DispatchAsync(
//             CreateCommand(),
//             CancellationToken.None);
//
//         Assert.Equal(2, result.RecipientCount);
//         Assert.Equal(2, sender.Messages.Count);
//     }
//
//     [Fact]
//     public async Task Dispatch_WhenExpoReportsUnregisteredDevice_DeactivatesIt()
//     {
//         ExpoPushToken token = ExpoPushToken.Create("ExpoPushToken[expired]");
//         var repository = new FakeDeviceRepository(token);
//         var sender = new FakePushSender
//         {
//             Tickets =
//             [
//                 new PushTicket(
//                     null,
//                     "error",
//                     "not registered",
//                     "DeviceNotRegistered")
//             ]
//         };
//         var dispatcher = new NotificationDispatcher(repository, sender);
//
//         await dispatcher.DispatchAsync(CreateCommand(), CancellationToken.None);
//
//         Assert.Equal(token, Assert.Single(repository.Deactivated));
//     }
//
//     private static DispatchNotificationCommand CreateCommand(
//         string? expoPushToken = null) =>
//         new(
//             "event-1",
//             "user-1",
//             expoPushToken,
//             "Title",
//             "Body",
//             DateTimeOffset.Parse(
//                 "2026-07-30T10:00:00Z",
//                 System.Globalization.CultureInfo.InvariantCulture),
//             new Dictionary<string, object?>());
//
//     private sealed class FakeDeviceRepository : IDeviceRegistrationRepository
//     {
//         private readonly IReadOnlyCollection<ExpoPushToken> _tokens;
//
//         public FakeDeviceRepository(params ExpoPushToken[] tokens)
//         {
//             _tokens = tokens;
//         }
//
//         public List<ExpoPushToken> Deactivated { get; } = [];
//
//         public Task<DeviceRegistration> RegisterAsync(
//             string userId,
//             ExpoPushToken token,
//             CancellationToken cancellationToken) =>
//             Task.FromResult(
//                 DeviceRegistration.Register(userId, token, DateTimeOffset.UtcNow));
//
//         public Task<IReadOnlyCollection<ExpoPushToken>> GetActiveTokensAsync(
//             string userId,
//             CancellationToken cancellationToken) =>
//             Task.FromResult(_tokens);
//
//         public Task DeactivateAsync(
//             ExpoPushToken token,
//             CancellationToken cancellationToken)
//         {
//             Deactivated.Add(token);
//             return Task.CompletedTask;
//         }
//     }
//
//     private sealed class FakePushSender : IPushNotificationSender
//     {
//         public List<PushMessage> Messages { get; } = [];
//
//         public IReadOnlyCollection<PushTicket>? Tickets { get; init; }
//
//         public Task<IReadOnlyCollection<PushTicket>> SendAsync(
//             IReadOnlyCollection<PushMessage> messages,
//             CancellationToken cancellationToken)
//         {
//             Messages.AddRange(messages);
//             IReadOnlyCollection<PushTicket> tickets = Tickets ??
//                 messages.Select(
//                     (_, index) => new PushTicket(
//                         $"ticket-{index}",
//                         "ok",
//                         null,
//                         null))
//                     .ToArray();
//
//             return Task.FromResult(tickets);
//         }
//     }
// }
