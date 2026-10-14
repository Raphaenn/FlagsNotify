using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.Interfaces;
using FlagsNotify.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FlagsNotify.Infrastructure.Persistence;

public sealed class DeviceRegistrationRepository : IDeviceRepository
{
    private readonly NotificationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DeviceRegistrationRepository(
        NotificationDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<DeviceRegistration> RegisterAsync(
        string userId,
        ExpoPushToken token,
        CancellationToken cancellationToken)
    {
        DeviceRegistration? existing = await _dbContext.DeviceRegistrations
            .SingleOrDefaultAsync(
                device => device.Token == token,
                cancellationToken);

        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (existing is not null)
        {
            existing.Refresh(userId, now);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }

        DeviceRegistration device = DeviceRegistration.Register(userId, token, now);
        _dbContext.DeviceRegistrations.Add(device);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return device;
    }

    public async Task<IReadOnlyCollection<ExpoPushToken>> GetActiveTokensAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.DeviceRegistrations
            .AsNoTracking()
            .Where(device => device.UserId == userId && device.IsActive)
            .Select(device => device.Token)
            .ToArrayAsync(cancellationToken);
    }

    public async Task DeactivateAsync(
        ExpoPushToken token,
        CancellationToken cancellationToken)
    {
        DeviceRegistration? device = await _dbContext.DeviceRegistrations
            .SingleOrDefaultAsync(
                registration => registration.Token == token,
                cancellationToken);

        if (device is null)
        {
            return;
        }

        device.Deactivate(_timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
