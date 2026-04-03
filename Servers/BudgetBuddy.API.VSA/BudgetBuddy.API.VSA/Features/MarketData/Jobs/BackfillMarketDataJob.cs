using BudgetBuddy.API.VSA.Common.Infrastructure.BackgroundJobs;
using BudgetBuddy.API.VSA.Features.MarketData.Services;
using Quartz;

namespace BudgetBuddy.API.VSA.Features.MarketData.Jobs;

/// <summary>
/// Detects and fills gaps in historical price and FX rate snapshots.
/// Runs daily at off-peak hours. Safe to re-run — skips data already present.
/// Handles first-run backfill and new investment/currency additions automatically.
/// </summary>
[DisallowConcurrentExecution]
public class BackfillMarketDataJob(
    IServiceScopeFactory scopeFactory,
    ILogger<BackfillMarketDataJob> logger) : ScheduledJobBase(logger)
{
    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var backfillService = scope.ServiceProvider.GetRequiredService<IMarketDataBackfillService>();

        await backfillService.BackfillAllMissingAsync(context.CancellationToken);
    }
}
