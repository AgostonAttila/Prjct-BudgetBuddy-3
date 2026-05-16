using BudgetBuddy.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Quartz;

namespace BudgetBuddy.Shared.Infrastructure.BackgroundJobs;

/// <summary>
/// S-08: Purges AuditLog rows older than <c>AuditLog:RetentionDays</c> (default 90 days).
/// Generic over TDbContext so each service registers its own instance without cross-schema access.
/// </summary>
[DisallowConcurrentExecution]
public class AuditLogRetentionJob<TDbContext>(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AuditLogRetentionJob<TDbContext>> logger)
    : ScheduledJobBase(logger)
    where TDbContext : AppDbContext
{
    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        var retentionDays = configuration.GetValue<int>("AuditLog:RetentionDays", 90);

        await using var scope = scopeFactory.CreateAsyncScope();
        var clock  = scope.ServiceProvider.GetRequiredService<IClock>();
        var cutoff = clock.GetCurrentInstant() - Duration.FromDays(retentionDays);

        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var deleted = await db.AuditLogs
            .Where(a => a.CreatedAt < cutoff)
            .ExecuteDeleteAsync(context.CancellationToken);

        if (deleted > 0)
        {
            Logger.LogInformation(
                "AuditLog retention: deleted {Count} records older than {CutoffDate} (retentionDays={Days}) from {DbContext}",
                deleted, cutoff, retentionDays, typeof(TDbContext).Name);
        }
    }
}
