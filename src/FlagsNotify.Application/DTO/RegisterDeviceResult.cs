namespace FlagsNotify.Application.DTO;

public sealed record RegisterDeviceResult(
    Guid Id,
    string UserId,
    string ExpoPushToken,
    bool IsActive);