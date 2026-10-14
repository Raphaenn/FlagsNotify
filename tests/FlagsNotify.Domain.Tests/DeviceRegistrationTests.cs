using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.ValueObjects;

namespace FlagsNotify.Domain.Tests;

public sealed class DeviceRegistrationTests
{
    [Fact]
    public void Refresh_ReactivatesAndReassignsDevice()
    {
        DateTimeOffset registeredAt = DateTimeOffset.Parse(
            "2026-07-30T10:00:00Z",
            System.Globalization.CultureInfo.InvariantCulture);
        DateTimeOffset updatedAt = registeredAt.AddHours(1);
        DeviceRegistration device = DeviceRegistration.Register(
            "old-user",
            ExpoPushToken.Create("ExpoPushToken[device]"),
            registeredAt);
        device.Deactivate(registeredAt.AddMinutes(30));

        device.Refresh("new-user", updatedAt);

        Assert.True(device.IsActive);
        Assert.Equal("new-user", device.UserId);
        Assert.Equal(updatedAt, device.UpdatedAt);
    }
}
