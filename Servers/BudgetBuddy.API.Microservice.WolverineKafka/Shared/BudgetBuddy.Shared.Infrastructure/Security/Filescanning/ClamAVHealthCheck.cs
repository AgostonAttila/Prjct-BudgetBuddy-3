using Microsoft.Extensions.Diagnostics.HealthChecks;
using nClam;

namespace BudgetBuddy.Shared.Infrastructure.Security.Filescanning;

/// <summary>
/// A-11: Health check for the ClamAV antivirus daemon.
/// Registers under tag "infrastructure" so it appears in /health but not /health/ready
/// (a slow ClamAV startup should not block service readiness).
/// </summary>
public sealed class ClamAVHealthCheck(IConfiguration configuration) : IHealthCheck
{
    private readonly string _serverUrl = configuration["ClamAV:ServerUrl"] ?? "localhost";
    private readonly int    _port      = configuration.GetValue<int>("ClamAV:Port", 3310);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client  = new ClamClient(_serverUrl, _port);
            var pingOk  = await client.PingAsync(cancellationToken);

            return pingOk
                ? HealthCheckResult.Healthy($"ClamAV reachable at {_serverUrl}:{_port}")
                : HealthCheckResult.Unhealthy($"ClamAV ping failed at {_serverUrl}:{_port}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"ClamAV unreachable at {_serverUrl}:{_port}", ex);
        }
    }
}
