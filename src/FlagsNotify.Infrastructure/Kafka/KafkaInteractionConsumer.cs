using System.Text.Json;
using Confluent.Kafka;
using FlagsNotify.Application.DTO;
using FlagsNotify.Application.Notifications.Commands;
using FlagsNotify.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlagsNotify.Infrastructure.Kafka;

/*
 * Background work - Get kafka event and dispatch a push notification based on type
 */
public sealed class KafkaInteractionConsumer : BackgroundService
{
    private readonly KafkaOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly InteractionEventDeserializer _deserializer;
    private readonly ILogger<KafkaInteractionConsumer> _logger;

    public KafkaInteractionConsumer(
        IOptions<KafkaOptions> options,
        IServiceScopeFactory scopeFactory,
        InteractionEventDeserializer deserializer,
        ILogger<KafkaInteractionConsumer> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _deserializer = deserializer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Consumidor Kafka desabilitado por configuração.");
            return;
        }

        ConsumerConfig config = CreateConsumerConfig();
        using IConsumer<Ignore, string> consumer = new ConsumerBuilder<Ignore, string>(config)
            .SetErrorHandler((_, error) =>
                _logger.LogError(
                    "Erro Kafka {Code}: {Reason}",
                    error.Code,
                    error.Reason))
            .Build();

        consumer.Subscribe(_options.Topics);
        _logger.LogInformation(
            "Consumindo os tópicos Kafka {Topics} com o grupo {GroupId}.",
            string.Join(", ", _options.Topics),
            _options.GroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ConsumeNextAsync(consumer, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Consumidor Kafka finalizado.");
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task ConsumeNextAsync(
        IConsumer<Ignore, string> consumer,
        CancellationToken cancellationToken)
    {
        ConsumeResult<Ignore, string> result = consumer.Consume(cancellationToken);

        try
        {
            DispatchNotificationCommand command =
                _deserializer.Deserialize(result.Message.Value);

            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            DispatchNotificationCommandHandler commandHandler =
                scope.ServiceProvider.GetRequiredService<DispatchNotificationCommandHandler>();

            DispatchNotificationResult dispatchResult = await commandHandler.DispatchAsync(
                command,
                cancellationToken);

            consumer.StoreOffset(result);
            consumer.Commit(result);

            _logger.LogInformation(
                "Evento {EventId} processado em {TopicPartitionOffset}; {RecipientCount} destinatário(s).",
                command.EventId,
                result.TopicPartitionOffset,
                dispatchResult.RecipientCount);
        }
        catch (Exception exception) when (
            exception is JsonException or DomainValidationException)
        {
            consumer.StoreOffset(result);
            consumer.Commit(result);

            _logger.LogError(
                exception,
                "Evento inválido descartado em {TopicPartitionOffset}.",
                result.TopicPartitionOffset);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            consumer.Seek(result.TopicPartitionOffset);
            _logger.LogError(
                exception,
                "Falha transitória em {TopicPartitionOffset}; o evento será tentado novamente.",
                result.TopicPartitionOffset);

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    private ConsumerConfig CreateConsumerConfig()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            ClientId = _options.ClientId,
            AutoOffsetReset = _options.AutoOffsetReset,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            SecurityProtocol = _options.SecurityProtocol
        };

        if (_options.SaslMechanism is not null)
        {
            config.SaslMechanism = _options.SaslMechanism;
            config.SaslUsername = _options.SaslUsername;
            config.SaslPassword = _options.SaslPassword;
        }

        return config;
    }
}
