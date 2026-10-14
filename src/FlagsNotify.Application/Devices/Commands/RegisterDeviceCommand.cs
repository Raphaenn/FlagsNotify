namespace FlagsNotify.Application.Devices.Commands;

public sealed record RegisterDeviceCommand(string UserId, string ExpoPushToken);
