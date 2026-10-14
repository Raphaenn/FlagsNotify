namespace FlagsNotify.Application.DTO;

public sealed record DispatchNotificationResult(
    Guid NotificationId,
    int RecipientCount,
    IReadOnlyCollection<string> TicketIds);