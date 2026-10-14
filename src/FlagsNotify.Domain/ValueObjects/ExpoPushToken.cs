namespace FlagsNotify.Domain.ValueObjects;

public sealed record ExpoPushToken
{
    private ExpoPushToken(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ExpoPushToken Create(string value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        bool hasValidPrefix =
            normalized.StartsWith("ExpoPushToken[", StringComparison.Ordinal) ||
            normalized.StartsWith("ExponentPushToken[", StringComparison.Ordinal);

        if (!hasValidPrefix || !normalized.EndsWith(']') || normalized.Length > 256)
        {
            throw new DomainValidationException("O ExpoPushToken informado é inválido.");
        }

        return new ExpoPushToken(normalized);
    }

    public override string ToString() => Value;
}
