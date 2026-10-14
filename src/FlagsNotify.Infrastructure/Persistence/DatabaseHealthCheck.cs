using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlagsNotify.Infrastructure.Persistence;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly NotificationDbContext _dbContext;

    public DatabaseHealthCheck(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        bool canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy("PostgreSQL disponível.")
            : HealthCheckResult.Unhealthy("Não foi possível acessar o PostgreSQL.");
    }
}
