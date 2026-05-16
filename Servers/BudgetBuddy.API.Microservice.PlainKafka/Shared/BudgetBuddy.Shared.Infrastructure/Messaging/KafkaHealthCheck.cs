using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BudgetBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// Health check that verifies Kafka broker connectivity by fetching cluster metadata.
/// Registered with tags ["ready", "live"] so it appears in /health/ready and /health/live.
/// The AdminClient is created once and reused across health check calls — IHealthCheck
/// instances are singletons in ASP.NET Core's health check framework.
/// </summary>
public sealed class KafkaHealthCheck : IHealthCheck, IDisposable
{
    private readonly IAdminClient _adminClient;
    private readonly string _bootstrapServers;

    public KafkaHealthCheck(KafkaSettings settings)
    {
        _bootstrapServers = settings.BootstrapServers;
        var adminConfig = new AdminClientConfig { BootstrapServers = settings.BootstrapServers };
        settings.ApplySecurity(adminConfig);
        _adminClient = new AdminClientBuilder(adminConfig).Build();
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 5-second timeout — enough for a local/in-cluster broker, not too long for startup probes
            _adminClient.GetMetadata(timeout: TimeSpan.FromSeconds(5));
            return Task.FromResult(HealthCheckResult.Healthy(
                $"Kafka reachable at {_bootstrapServers}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Kafka unreachable at {_bootstrapServers}", ex));
        }
    }

    public void Dispose() => _adminClient.Dispose();
}
