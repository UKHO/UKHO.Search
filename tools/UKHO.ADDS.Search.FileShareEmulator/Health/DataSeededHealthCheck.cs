using Microsoft.Extensions.Diagnostics.HealthChecks;
using UKHO.ADDS.Search.FileShareEmulator.Infrastructure;

namespace UKHO.ADDS.Search.FileShareEmulator.Health;

public sealed class DataSeededHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!File.Exists("/data/.seed.complete"))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Seed sentinel not present."));
        }

        return Task.FromResult(BacpacImportState.Completed
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Bacpac import not completed."));
    }
}
