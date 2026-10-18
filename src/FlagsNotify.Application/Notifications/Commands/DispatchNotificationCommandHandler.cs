using FlagsNotify.Application.Abstractions;
using FlagsNotify.Application.DTO;
using FlagsNotify.Domain;
using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.Enuns;
using FlagsNotify.Domain.Interfaces;
using FlagsNotify.Domain.ValueObjects;

namespace FlagsNotify.Application.Notifications.Commands;

public sealed class DispatchNotificationCommandHandler
{
    private readonly IDeviceRepository _devicesRepository;
    private readonly INotificationRepository _notificationsRepository;
    private readonly IPushNotificationSender _pushSender;
    private readonly TimeProvider _timeProvider;

    public DispatchNotificationCommandHandler(
        IDeviceRepository devicesRepository,
        INotificationRepository notificationsRepository,
        IPushNotificationSender pushSender,
        TimeProvider timeProvider
        )
    {
        _devicesRepository = devicesRepository;
        _notificationsRepository = notificationsRepository;
        _pushSender = pushSender;
        _timeProvider = timeProvider;
    }

    public async Task<DispatchNotificationResult> DispatchAsync(
        DispatchNotificationCommand command,
        CancellationToken cancellationToken)
    {
        // todo -> By event type we should define notification title and body
        // todo -> search user name by id. 
        
        Notification? existing = await _notificationsRepository.FindByEventIdAsync(
            command.EventId,
            cancellationToken);
        if (existing?.DeliveryCompletedAt is not null)
        {
            return new DispatchNotificationResult(existing.Id, 0, new List<string>());
        }

        string defaultTitle = command.EventType switch
        {
            EventTypes.HandleLike => $"You got a like from {command.SenderName}",
            EventTypes.ReceiveDm => $"New dm message from {command.SenderName}",
            _ => "New notification"
        };

        string title = string.IsNullOrWhiteSpace(command.Title)
            ? defaultTitle
            : command.Title;
        string? route = ReadRoute(command.Data);
        
        Notification notification = existing ?? Notification.Create(
                command.EventId,
                command.RecipientId,
                command.SenderId,
                command.SenderName,
                command.EventType,
                title,
                command.Body ?? "New notification",
                route,
                command.OccurredAt);

        if (existing is null)
        {
            await _notificationsRepository.AddAsync(notification, cancellationToken);
        }

        IReadOnlyCollection<ExpoPushToken> tokens = await ResolveRecipientsAsync(
            command.RecipientId,
            null,
            cancellationToken);

        if (tokens.Count == 0)
        {
            notification.MarkDeliveryCompleted(_timeProvider.GetUtcNow());
            await _notificationsRepository.UpdateAsync(notification, cancellationToken);
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

        notification.MarkDeliveryCompleted(_timeProvider.GetUtcNow());
        await _notificationsRepository.UpdateAsync(notification, cancellationToken);

        return new DispatchNotificationResult(notification.Id, messages.Length, ticketIds);
    }

    private static string? ReadRoute(IReadOnlyDictionary<string, object?> data)
    {
        foreach (string key in new[] { "route", "url" })
        {
            if (!data.TryGetValue(key, out object? value) || value is null)
            {
                continue;
            }

            string? route = value switch
            {
                System.Text.Json.JsonElement element when element.ValueKind ==
                    System.Text.Json.JsonValueKind.String => element.GetString(),
                string stringValue => stringValue,
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(route))
            {
                return route;
            }
        }

        return null;
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
