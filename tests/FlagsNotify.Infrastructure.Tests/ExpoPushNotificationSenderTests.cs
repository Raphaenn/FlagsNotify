// using System.Net;
// using System.Text;
// using FlagsNotify.Application.Abstractions;
// using FlagsNotify.Infrastructure.Push;
// using Microsoft.Extensions.Options;
//
// namespace FlagsNotify.Infrastructure.Tests;
//
// public sealed class ExpoPushNotificationSenderTests
// {
//     [Fact]
//     public async Task SendAsync_SuccessfulResponse_ReturnsTicket()
//     {
//         const string Response = """
//             {
//               "data": [
//                 { "status": "ok", "id": "ticket-123" }
//               ]
//             }
//             """;
//         var messageHandler = new StubHttpMessageHandler(
//             HttpStatusCode.OK,
//             Response);
//         var client = new HttpClient(messageHandler);
//         var sender = new ExpoPushNotificationSender(
//             client,
//             Options.Create(
//                 new ExpoPushOptions
//                 {
//                     RetryAttempts = 0
//                 }));
//
//         IReadOnlyCollection<PushTicket> tickets = await sender.SendAsync(
//             [
//                 new PushMessage(
//                     "ExpoPushToken[device]",
//                     "Title",
//                     "Body",
//                     new Dictionary<string, object?>())
//             ],
//             CancellationToken.None);
//
//         PushTicket ticket = Assert.Single(tickets);
//         Assert.Equal("ticket-123", ticket.Id);
//         Assert.Contains("ExpoPushToken[device]", messageHandler.RequestBody);
//     }
//
//     [Fact]
//     public async Task SendAsync_DeviceNotRegistered_MapsErrorCode()
//     {
//         const string Response = """
//             {
//               "data": [
//                 {
//                   "status": "error",
//                   "message": "not registered",
//                   "details": { "error": "DeviceNotRegistered" }
//                 }
//               ]
//             }
//             """;
//         var client = new HttpClient(
//             new StubHttpMessageHandler(HttpStatusCode.OK, Response));
//         var sender = new ExpoPushNotificationSender(
//             client,
//             Options.Create(
//                 new ExpoPushOptions
//                 {
//                     RetryAttempts = 0
//                 }));
//
//         IReadOnlyCollection<PushTicket> tickets = await sender.SendAsync(
//             [
//                 new PushMessage(
//                     "ExpoPushToken[device]",
//                     "Title",
//                     "Body",
//                     new Dictionary<string, object?>())
//             ],
//             CancellationToken.None);
//
//         Assert.Equal("DeviceNotRegistered", Assert.Single(tickets).ErrorCode);
//     }
//
//     private sealed class StubHttpMessageHandler : HttpMessageHandler
//     {
//         private readonly HttpStatusCode _statusCode;
//         private readonly string _response;
//
//         public StubHttpMessageHandler(
//             HttpStatusCode statusCode,
//             string response)
//         {
//             _statusCode = statusCode;
//             _response = response;
//         }
//
//         public string RequestBody { get; private set; } = string.Empty;
//
//         protected override async Task<HttpResponseMessage> SendAsync(
//             HttpRequestMessage request,
//             CancellationToken cancellationToken)
//         {
//             RequestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
//             return new HttpResponseMessage(_statusCode)
//             {
//                 Content = new StringContent(
//                     _response,
//                     Encoding.UTF8,
//                     "application/json")
//             };
//         }
//     }
// }
