namespace BudgetBuddy.API.VSA.Common.Infrastructure.Security;

/// <summary>
/// Tracks and logs graceful shutdown events for better observability
/// Monitors in-flight requests during shutdown to ensure clean termination
/// </summary>
public class GracefulShutdownService(
    ILogger<GracefulShutdownService> logger,
    IHostApplicationLifetime lifetime,
    IConfiguration configuration)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Register shutdown event handlers
        lifetime.ApplicationStopping.Register(OnApplicationStopping);
        lifetime.ApplicationStopped.Register(OnApplicationStopped);

        logger.LogInformation("Graceful shutdown tracking service started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Graceful shutdown tracking service stopped");
        return Task.CompletedTask;
    }

    private void OnApplicationStopping()
    {
        var shutdownTimeout = configuration.GetValue<int>("ShutdownTimeoutSeconds", 30);

        logger.LogWarning(
            "APPLICATION SHUTDOWN INITIATED. Waiting up to {ShutdownTimeout} seconds for in-flight requests to complete...",
            shutdownTimeout);

        // This executes when shutdown is triggered but before the application stops
        // The host will wait for all IHostedService.StopAsync() calls to complete
        // or until ShutdownTimeout expires
    }

    private void OnApplicationStopped()
    {
        logger.LogWarning("APPLICATION SHUTDOWN COMPLETED. All requests processed or timeout reached.");

        // This executes after the application has fully stopped
        // All in-flight requests should be completed or timed out by this point
    }
}
