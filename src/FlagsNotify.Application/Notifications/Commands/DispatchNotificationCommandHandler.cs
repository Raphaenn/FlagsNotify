using FlagsNotify.Application.Abstractions;
using FlagsNotify.Application.DTO;
using FlagsNotify.Domain;
using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.Interfaces;
using FlagsNotify.Domain.ValueObjects;

namespace FlagsNotify.Application.Notifications.Commands;

public sealed class DispatchNotificationCommandHandler
{
    private readonly IDeviceRepository _devicesRepository;
    private readonly IPushNotificationSender _pushSender;

    public DispatchNotificationCommandHandler(
        IDeviceRepository devicesRepository,
        IPushNotificationSender pushSender)
    {
        _devicesRepository = devicesRepository;
        _pushSender = pushSender;
    }

    public async Task<DispatchNotificationResult> DispatchAsync(
        DispatchNotificationCommand command,
        CancellationToken cancellationToken)
    {
        Notification notification = Notification.Create(
            command.EventId,
            command.UserId,
            command.Title,
            command.Body,
            command.OccurredAt);

        IReadOnlyCollection<ExpoPushToken> tokens = await ResolveRecipientsAsync(
            command.UserId,
            command.ExpoPushToken,
            cancellationToken);

        if (tokens.Count == 0)
        {
            return new DispatchNotificationResult(notification.Id, 0, new List<string>());
        }

        var messages = tokens
            .Distinct()
            .Select(token => new PushMessage(
                token.Value,
                notification.Title,
                notification.Body,
                new Dictionary<string, object?>(command.Data)
                {
                    ["notificationId"] = notification.Id,
                    ["eventId"] = notification.EventId,
                    ["occurredAt"] = notification.OccurredAt
                }))
            .ToArray();

        IReadOnlyCollection<PushTicket> tickets = await _pushSender.SendAsync(
            messages,
            cancellationToken);

        PushTicket[] ticketArray = tickets.ToArray();
        for (int index = 0; index < ticketArray.Length && index < messages.Length; index++)
        {
            if (string.Equals(
                    ticketArray[index].ErrorCode,
                    "DeviceNotRegistered",
                    StringComparison.Ordinal))
            {
                await _devicesRepository.DeactivateAsync(
                    ExpoPushToken.Create(messages[index].To),
                    cancellationToken);
            }
        }

        string[] ticketIds = ticketArray
            .Where(ticket => !string.IsNullOrWhiteSpace(ticket.Id))
            .Select(ticket => ticket.Id!)
            .ToArray();

        return new DispatchNotificationResult(notification.Id, messages.Length, ticketIds);
    }

    private async Task<IReadOnlyCollection<ExpoPushToken>> ResolveRecipientsAsync(
        string? userId,
        string? token,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            return Array.AsReadOnly(new[]
            {
                ExpoPushToken.Create(token)
            });
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new DomainValidationException(
                "Informe userId ou expoPushToken para definir o destinatário.");
        }

        return await _devicesRepository.GetActiveTokensAsync(userId.Trim(), cancellationToken);
    }
}
