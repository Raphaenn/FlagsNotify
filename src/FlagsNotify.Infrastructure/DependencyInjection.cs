using FlagsNotify.Application.Abstractions;
using FlagsNotify.Domain.Interfaces;
using FlagsNotify.Infrastructure.Kafka;
using FlagsNotify.Infrastructure.Persistence;
using FlagsNotify.Infrastructure.Push;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlagsNotify.Infrastructure;

public static class DependencyInjection
{
    private readonly static string[] LiveTags = { "live" };
    private readonly static string[] ReadyTags = { "ready" };
    
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string? connectionString =
            configuration.GetConnectionString("PostgresRepository");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string 'PostgresRepository' não foi configurada.");
        }

        services.AddSingleton(TimeProvider.System);

        services.AddDbContext<NotificationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable(
                    "__flagsnotify_migrations_history")));

        services.AddScoped<IDeviceRepository, DeviceRegistrationRepository>();

        services.AddOptions<ExpoPushOptions>()
            .Bind(configuration.GetSection(ExpoPushOptions.SectionName))
            .Validate(
                options => Uri.TryCreate(
                    options.Endpoint,
                    UriKind.Absolute,
                    out Uri? endpoint) &&
                    endpoint.Scheme == Uri.UriSchemeHttps,
                "ExpoPush:Endpoint deve ser uma URL HTTPS absoluta.")
            .Validate(
                options => options.MaxBatchSize is >= 1 and <= 100,
                "ExpoPush:MaxBatchSize deve estar entre 1 e 100.")
            .ValidateOnStart();

        services.AddHttpClient<IPushNotificationSender, PushNotificationSender>();

        services.AddOptions<KafkaOptions>()
            .Bind(configuration.GetSection(KafkaOptions.SectionName))
            .Validate(
                options => !options.Enabled ||
                    !string.IsNullOrWhiteSpace(options.BootstrapServers),
                "Kafka:BootstrapServers é obrigatório quando Kafka está habilitado.")
            .Validate(
                options => !options.Enabled || options.Topics.Length > 0,
                "Ao menos um tópico Kafka deve ser configurado.")
            .ValidateOnStart();

        services.AddSingleton<InteractionEventDeserializer>();
        services.AddHostedService<KafkaInteractionConsumer>();

        services.AddHealthChecks()
            .AddCheck(
                "self",
                () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(),
                tags: LiveTags)
            .AddCheck<DatabaseHealthCheck>("database", tags: ReadyTags);

        return services;
    }

    public static async Task ApplyDatabaseMigrationsAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        NotificationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
