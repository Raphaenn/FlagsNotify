namespace FlagsNotify.Application.Abstractions;

public interface IPushNotificationSender
{
    Task<IReadOnlyCollection<PushTicket>> SendAsync(
        IReadOnlyCollection<PushMessage> messages,
        CancellationToken cancellationToken);
}

public sealed record PushMessage(
    string To,
    string Title,
    string Body,
    IReadOnlyDictionary<string, object?> Data);

public sealed record PushTicket(
    string? Id,
    string Status,
    string? Error,
    string? ErrorCode);
