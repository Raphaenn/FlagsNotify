using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.ValueObjects;

namespace FlagsNotify.Domain.Interfaces;

public interface IDeviceRepository
{
    Task<DeviceRegistration> RegisterAsync(
        string userId,
        ExpoPushToken token,
        CancellationToken cancellationToken);
    
    Task<IReadOnlyCollection<ExpoPushToken>> GetActiveTokensAsync(
        string userId,
        CancellationToken cancellationToken);

    Task DeactivateAsync(
        ExpoPushToken token,
        CancellationToken cancellationToken);
}