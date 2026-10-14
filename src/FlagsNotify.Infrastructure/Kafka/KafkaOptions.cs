using Confluent.Kafka;

namespace FlagsNotify.Infrastructure.Kafka;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; init; } = true;

    public string BootstrapServers { get; init; } = "localhost:9092";

    public string GroupId { get; init; } = "flags-notify";

    public string ClientId { get; init; } = "flags-notify-api";

    public string Topics { get; init; } = "interactions-events-dev";

    public AutoOffsetReset AutoOffsetReset { get; init; } = AutoOffsetReset.Earliest;

    public SecurityProtocol SecurityProtocol { get; init; } = SecurityProtocol.Plaintext;

    public SaslMechanism? SaslMechanism { get; init; }

    public string? SaslUsername { get; init; }

    public string? SaslPassword { get; init; }
}
