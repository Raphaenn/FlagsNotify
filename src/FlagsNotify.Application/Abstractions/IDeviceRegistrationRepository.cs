using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.ValueObjects;

namespace FlagsNotify.Application.Abstractions;

public interface IDeviceRegistrationRepository
{
    Task<IReadOnlyCollection<ExpoPushToken>> GetActiveTokensAsync(
        string userId,
        CancellationToken cancellationToken);

    Task DeactivateAsync(
        ExpoPushToken token,
        CancellationToken cancellationToken);
}
