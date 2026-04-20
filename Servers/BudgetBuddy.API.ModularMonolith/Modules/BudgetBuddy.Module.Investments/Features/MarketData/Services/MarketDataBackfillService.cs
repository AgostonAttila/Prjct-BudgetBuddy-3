using BudgetBuddy.Shared.Contracts.Transactions;
using BudgetBuddy.Shared.Kernel.Entities;
using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Infrastructure.Financial;
using BudgetBuddy.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace BudgetBuddy.Module.Investments.Features.MarketData.Services;

public class MarketDataBackfillService(
    InvestmentsDbContext db,
    ITransactionQueryService transactionQueryService,
    IHistoricalPriceService historicalPriceService,
    IFxHistoricalProvider fxHistory,
    IClock clock,
    ILogger<MarketDataBackfillService> logger) : IMarketDataBackfillService
{
    private readonly LocalDate _today = clock.GetCurrentInstant().InUtc().Date;

    public async Task BackfillAllMissingAsync(CancellationToken cancellationToken = default)
    {
        await BackfillPricesAsync(cancellationToken);
        await BackfillFxRatesAsync(cancellationToken);
    }

    // -------------------------------------------------------------------------
    // Price backfill
    // -------------------------------------------------------------------------

    private async Task BackfillPricesAsync(CancellationToken cancellationToken)
    {
        var investments = await db.Investments
            .GroupBy(i => new { i.Symbol, i.Type })
            .Select(g => new { g.Key.Symbol, g.Key.Type, EarliestPurchase = g.Min(i => i.PurchaseDate) })
            .ToListAsync(cancellationToken);

        var existingCoverage = await db.PriceSnapshots
            .GroupBy(p => p.Symbol)
            .Select(g => new { Symbol = g.Key, EarliestDate = g.Min(p => p.Date) })
            .ToDictionaryAsync(x => x.Symbol, x => x.EarliestDate, cancellationToken);

        var symbolsNeedingBackfill = investments
            .Where(i => !existingCoverage.TryGetValue(i.Symbol, out var earliest)
                        || earliest > i.EarliestPurchase)
            .ToList();

        if (symbolsNeedingBackfill.Count == 0)
        {
            logger.LogInformation("Price backfill: all symbols up to date");
            return;
        }

        logger.LogInformation("Price backfill: {Count} symbols need historical data", symbolsNeedingBackfill.Count);

        foreach (var investment in symbolsNeedingBackfill)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var existingDates = await db.PriceSnapshots
                    .Where(p => p.Symbol == investment.Symbol)
                    .Select(p => p.Date)
                    .ToHashSetAsync(cancellationToken);

                var allPrices = await historicalPriceService.GetDailyClosePricesAsync(
                    investment.Symbol, investment.Type, investment.EarliestPurchase, cancellationToken);

                var toInsert = allPrices
                    .Where(kv => kv.Key >= investment.EarliestPurchase
                                 && kv.Key <= _today
                                 && !existingDates.Contains(kv.Key))
                    .Select(kv => new PriceSnapshot
                    {
                        Symbol = investment.Symbol,
                        Date = kv.Key,
                        ClosePrice = kv.Value,
                        Currency = "USD",
                        Source = investment.Type == InvestmentType.Crypto ? "coingecko" : "yahoo",
                        CreatedAt = clock.GetCurrentInstant()
                    })
                    .ToList();

                if (toInsert.Count > 0)
                {
                    db.PriceSnapshots.AddRange(toInsert);
                    await db.SaveChangesAsync(cancellationToken);
                    logger.LogInformation("Price backfill: inserted {Count} days for {Symbol}", toInsert.Count, investment.Symbol);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Price backfill failed for {Symbol}, will retry on next run", investment.Symbol);
            }
        }
    }

    // -------------------------------------------------------------------------
    // FX rate backfill
    // -------------------------------------------------------------------------

    private async Task BackfillFxRatesAsync(CancellationToken cancellationToken)
    {
        var earliestTransaction = await transactionQueryService
            .GetEarliestTransactionDateAsync(cancellationToken);

        var earliestInvestment = await db.Investments
            .MinAsync(i => (LocalDate?)i.PurchaseDate, cancellationToken);

        var earliestNeeded = new[] { earliestTransaction, earliestInvestment }
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .DefaultIfEmpty(_today)
            .Min();

        var earliestExisting = await db.ExchangeRateSnapshots
            .MinAsync(e => (LocalDate?)e.Date, cancellationToken);

        if (earliestExisting.HasValue && earliestExisting.Value <= earliestNeeded)
        {
            logger.LogInformation("FX backfill: coverage starts {Date}, no backfill needed", earliestExisting.Value);
            return;
        }

        var backfillFrom = earliestNeeded;
        var backfillTo = earliestExisting.HasValue
            ? earliestExisting.Value.PlusDays(-1)
            : _today.PlusDays(-1);

        if (backfillFrom > backfillTo)
            return;

        logger.LogInformation("FX backfill: fetching {From} to {To}", backfillFrom, backfillTo);

        try
        {
            var historicalRates = await fxHistory.GetDailyRatesAsync(
                backfillFrom, backfillTo, "USD", cancellationToken);

            if (historicalRates.Count == 0)
            {
                logger.LogWarning("FX backfill: no data returned from Frankfurter");
                return;
            }

            var existingDates = await db.ExchangeRateSnapshots
                .Select(e => e.Date)
                .ToHashSetAsync(cancellationToken);

            var now = clock.GetCurrentInstant();

            var snapshots = historicalRates
                .Where(kv => !existingDates.Contains(kv.Key))
                .SelectMany(kv => kv.Value
                    .Where(rate => rate.Value > 0)
                    .Select(rate => new ExchangeRateSnapshot
                    {
                        Currency = rate.Key,
                        Date = kv.Key,
                        RateToUsd = 1m / rate.Value,
                        CreatedAt = now
                    }))
                .ToList();

            if (snapshots.Count > 0)
            {
                db.ExchangeRateSnapshots.AddRange(snapshots);
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("FX backfill: inserted {Count} rate entries for {Days} days",
                    snapshots.Count, historicalRates.Count);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "FX backfill failed, will retry on next run");
        }
    }
}
