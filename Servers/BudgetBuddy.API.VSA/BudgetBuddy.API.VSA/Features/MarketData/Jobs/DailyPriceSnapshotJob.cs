using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure.BackgroundJobs;
using BudgetBuddy.API.VSA.Common.Infrastructure.Financial;
using BudgetBuddy.API.VSA.Common.Infrastructure.Persistence;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Quartz;

namespace BudgetBuddy.API.VSA.Features.MarketData.Jobs;

/// <summary>
/// Fetches and persists daily closing prices for all symbols held in user portfolios.
/// Runs after US market close (~22:00 UTC). Prices are stored in USD.
/// </summary>
[DisallowConcurrentExecution]
public class DailyPriceSnapshotJob(
    IServiceScopeFactory scopeFactory,
    IClock clock,
    ILogger<DailyPriceSnapshotJob> logger) : ScheduledJobBase(logger)
{
    protected override async Task ExecuteAsync(IJobExecutionContext context)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var priceService = scope.ServiceProvider.GetRequiredService<IPriceService>();

        var today = clock.GetCurrentInstant().InUtc().Date;

        var symbols = await db.Investments
            .Select(i => new { i.Symbol, i.Type })
            .Distinct()
            .ToListAsync(context.CancellationToken);

        if (symbols.Count == 0)
        {
            Logger.LogInformation("No investment symbols found, skipping price snapshot");
            return;
        }

        var prices = await priceService.GetBatchPricesAsync(
            symbols.Select(s => (s.Symbol, s.Type)).ToList(),
            currencyCode: "USD",
            cancellationToken: context.CancellationToken);

        var existing = await db.PriceSnapshots
            .Where(p => p.Date == today)
            .Select(p => p.Symbol)
            .ToHashSetAsync(context.CancellationToken);

        var typeBySymbol = symbols.ToDictionary(s => s.Symbol, s => s.Type);

        var snapshots = prices
            .Where(kv => !existing.Contains(kv.Key))
            .Select(kv => new PriceSnapshot
            {
                Symbol = kv.Key,
                Date = today,
                ClosePrice = kv.Value,
                Currency = "USD",
                Source = typeBySymbol.GetValueOrDefault(kv.Key) == InvestmentType.Crypto
                    ? "coingecko"
                    : "yahoo",
                CreatedAt = clock.GetCurrentInstant()
            })
            .ToList();

        if (snapshots.Count > 0)
        {
            db.PriceSnapshots.AddRange(snapshots);
            await db.SaveChangesAsync(context.CancellationToken);
        }

        Logger.LogInformation(
            "Price snapshot saved: {Saved} symbols for {Date}, {Skipped} already existed",
            snapshots.Count, today, prices.Count - snapshots.Count);
    }
}
