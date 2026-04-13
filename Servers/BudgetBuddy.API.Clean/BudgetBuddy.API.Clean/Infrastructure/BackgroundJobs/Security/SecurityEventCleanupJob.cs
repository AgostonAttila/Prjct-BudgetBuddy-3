using BudgetBuddy.Infrastructure.BackgroundJobs;
using Quartz;

namespace BudgetBuddy.Infrastructure.BackgroundJobs.Security;

/// <summary>
/// Deletes old, already-reviewed security events to prevent unbounded table growth.
/// Retention window is configurable via BackgroundJobs:SecurityEventCleanup:RetentionDays.
/// Only reviewed events are deleted — unreviewed alerts are retained regardless of age.
/// </summary>
public class SecurityEventCleanupJob(
    ISecurityEventRepository securityEventRepo,
    SecurityEventCleanupJobSettings settings,
    ILogger<SecurityEventCleanupJob> logger) : ScheduledJobBase(logger)
{
    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        var cutoff = Instant.FromDateTimeUtc(DateTime.UtcNow.AddDays(-settings.RetentionDays));

        var deleted = await securityEventRepo.DeleteOldAsync(cutoff, context.CancellationToken);

        if (deleted > 0)
            Logger.LogInformation(
                "SecurityEventCleanup: deleted {Count} reviewed events older than {RetentionDays} days",
                deleted, settings.RetentionDays);
    }
}
