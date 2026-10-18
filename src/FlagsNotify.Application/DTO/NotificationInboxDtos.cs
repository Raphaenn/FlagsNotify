namespace FlagsNotify.Application.DTO;

public sealed record NotificationItemDto(
    Guid Id,
    string Type,
    string Title,
    string Description,
    string? SenderUserId,
    string? SenderUserName,
    DateTimeOffset OccurredAt,
    bool Read,
    string? Route);

public sealed record NotificationPageDto(
    IReadOnlyList<NotificationItemDto> Items,
    string? NextCursor);

public sealed record UnreadNotificationCountDto(int Count);

public sealed record MarkAllNotificationsReadDto(int UpdatedCount);
