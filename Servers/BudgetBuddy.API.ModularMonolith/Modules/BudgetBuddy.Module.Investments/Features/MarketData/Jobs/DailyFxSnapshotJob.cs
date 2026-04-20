using BudgetBuddy.Shared.Kernel.Entities;
using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Infrastructure.Financial;
using BudgetBuddy.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Quartz;

namespace BudgetBuddy.Module.Investments.Jobs;

/// <summary>
/// Persists daily FX rates normalized to USD (RateToUsd = how many USD equals 1 unit of currency).
/// Runs once daily after ECB publishes reference rates (~18:00 UTC).
/// Cross-rate formula: amount_in_target = amount × (RateToUsd[source] / RateToUsd[target])
/// </summary>
[DisallowConcurrentExecution]
public class DailyFxSnapshotJob(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    ILogger<DailyFxSnapshotJob> logger) : ScheduledJobBase(logger)
{
    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<InvestmentsDbContext>();
        var fxService = scope.ServiceProvider.GetRequiredService<ICurrencyConversionService>();

        var today = clock.GetCurrentInstant().InUtc().Date;

        // GetRatesAsync("USD") returns { "EUR": 0.92, "HUF": 370, ... }
        // meaning: 1 USD = 0.92 EUR, 1 USD = 370 HUF
        // RateToUsd = 1 / frankfurterRate  →  1 EUR = 1.087 USD, 1 HUF = 0.0027 USD
        var rates = await fxService.GetRatesAsync("USD", context.CancellationToken);

        var existing = await db.ExchangeRateSnapshots
            .Where(e => e.Date == today)
            .Select(e => e.Currency)
            .ToHashSetAsync(context.CancellationToken);

        var now = clock.GetCurrentInstant();

        var snapshots = rates
            .Where(kv => kv.Value > 0 && !existing.Contains(kv.Key))
            .Select(kv => new ExchangeRateSnapshot
            {
                Currency = kv.Key,
                Date = today,
                RateToUsd = 1m / kv.Value,
                CreatedAt = now
            })
            .ToList();

        // USD itself is the base (not returned by Frankfurter), add manually
        if (!existing.Contains("USD"))
            snapshots.Add(new ExchangeRateSnapshot
            {
                Currency = "USD",
                Date = today,
                RateToUsd = 1m,
                CreatedAt = now
            });

        if (snapshots.Count > 0)
        {
            db.ExchangeRateSnapshots.AddRange(snapshots);
            await db.SaveChangesAsync(context.CancellationToken);
        }

        Logger.LogInformation(
            "FX snapshot saved: {Saved} currencies for {Date}, {Skipped} already existed",
            snapshots.Count, today, rates.Count + 1 - snapshots.Count);
    }
}
