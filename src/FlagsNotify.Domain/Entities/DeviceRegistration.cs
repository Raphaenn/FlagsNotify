using FlagsNotify.Domain.ValueObjects;

namespace FlagsNotify.Domain.Entities;

public sealed class DeviceRegistration
{
    private DeviceRegistration()
    {
        UserId = string.Empty;
        Token = null!;
    }

    private DeviceRegistration(
        Guid id,
        string userId,
        ExpoPushToken token,
        DateTimeOffset registeredAt)
    {
        Id = id;
        UserId = userId;
        Token = token;
        IsActive = true;
        RegisteredAt = registeredAt;
        UpdatedAt = registeredAt;
    }

    public Guid Id { get; private set; }

    public string UserId { get; private set; }

    public ExpoPushToken Token { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static DeviceRegistration Register(
        string userId,
        ExpoPushToken token,
        DateTimeOffset now)
    {
        return new DeviceRegistration(Guid.NewGuid(), NormalizeUserId(userId), token, now);
    }

    public void Refresh(string userId, DateTimeOffset now)
    {
        UserId = NormalizeUserId(userId);
        IsActive = true;
        UpdatedAt = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAt = now;
    }

    private static string NormalizeUserId(string userId)
    {
        string normalized = userId?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > 200)
        {
            throw new DomainValidationException("O identificador do usuário deve ter entre 1 e 200 caracteres.");
        }

        return normalized;
    }
}
