using BudgetBuddy.Shared.Infrastructure.BackgroundJobs;
using BudgetBuddy.Shared.Infrastructure.Financial;
using BudgetBuddy.Shared.Infrastructure.Persistence;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Events.Investments;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Quartz;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Investments.Jobs;

/// <summary>
/// Fetches and persists daily closing prices for all symbols held in user portfolios.
/// Publishes MarketPriceUpdatedEvent via the Wolverine transactional outbox.
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
        var db           = scope.ServiceProvider.GetRequiredService<InvestmentsDbContext>();
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
                Symbol    = kv.Key,
                Date      = today,
                ClosePrice = kv.Value,
                Currency  = "USD",
                Source    = typeBySymbol.GetValueOrDefault(kv.Key) == InvestmentType.Crypto
                    ? "coingecko"
                    : "yahoo",
                CreatedAt = clock.GetCurrentInstant()
            })
            .ToList();

        if (snapshots.Count > 0)
        {
            db.PriceSnapshots.AddRange(snapshots);

            var outbox = scope.ServiceProvider.GetRequiredService<IDbContextOutbox>();
            outbox.Enroll(db);

            foreach (var snapshot in snapshots)
            {
                await outbox.SendAsync(new MarketPriceUpdatedEvent
                {
                    Symbol   = snapshot.Symbol,
                    Price    = snapshot.ClosePrice,
                    Currency = snapshot.Currency
                });
            }

            await outbox.SaveChangesAndFlushMessagesAsync(context.CancellationToken);
        }

        Logger.LogInformation(
            "Price snapshot saved: {Saved} symbols for {Date}, {Skipped} already existed",
            snapshots.Count, today, prices.Count - snapshots.Count);
    }
}
