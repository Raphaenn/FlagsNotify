using System.ComponentModel.DataAnnotations;

namespace FlagsNotify.Infrastructure.Push;

public sealed class ExpoPushOptions
{
    public const string SectionName = "ExpoPush";

    [Required]
    [Url]
    public string Endpoint { get; init; } = "https://exp.host/--/api/v2/push/send";

    public string? AccessToken { get; init; }

    [Range(1, 100)]
    public int MaxBatchSize { get; init; } = 100;

    [Range(0, 5)]
    public int RetryAttempts { get; init; } = 3;
}
