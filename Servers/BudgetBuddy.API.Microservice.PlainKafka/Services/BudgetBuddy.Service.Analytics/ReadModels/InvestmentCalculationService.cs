using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Shared.Messages.Contracts.Financial;
using BudgetBuddy.Shared.Messages.Contracts.Investments;
using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Service.Analytics.ReadModels;

/// <summary>
/// Analytics-local implementation of IInvestmentCalculationService.
/// Price resolution order: 1. Financial service HTTP (live) → 2. PriceSnapshotReadModel (Kafka) → 3. 0
/// </summary>
public class InvestmentCalculationService(
    AnalyticsDbContext db,
    ICurrencyConversionService currencyConversion,
    IPriceService? priceService,
    ILogger<InvestmentCalculationService> logger) : IInvestmentCalculationService
{
    public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(
        List<(string Symbol, InvestmentType Type)> symbolsWithTypes,
        string targetCurrency,
        Dictionary<string, decimal>? purchasePriceFallback = null,
        CancellationToken cancellationToken = default)
    {
        var symbols = symbolsWithTypes.Select(s => s.Symbol).Distinct().ToList();

        // 1. Live prices via Financial service HTTP
        if (priceService is not null)
        {
            try
            {
                var livePrices = await priceService.GetBatchPricesAsync(
                    symbolsWithTypes, targetCurrency, cancellationToken);

                if (livePrices.Count > 0)
                {
                    var missing = symbols.Where(s => !livePrices.ContainsKey(s)).ToList();
                    if (missing.Count == 0)
                    {
                        return livePrices;
                    }

                    var result = new Dictionary<string, decimal>(livePrices, StringComparer.OrdinalIgnoreCase);
                    var snapshots = await GetSnapshotPricesAsync(missing, targetCurrency, cancellationToken);
                    foreach (var (sym, price) in snapshots)
                    {
                        result[sym] = price;
                    }

                    foreach (var sym in missing.Where(s => !result.ContainsKey(s)))
                    {
                        result[sym] = 0m;
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Financial service unavailable, falling back to snapshots");
            }
        }
        else
        {
            logger.LogWarning("IPriceService not registered; falling back to snapshots");
        }

        // 2. PriceSnapshot (Kafka read model)
        var snapshotPrices = await GetSnapshotPricesAsync(symbols, targetCurrency, cancellationToken);
        if (snapshotPrices.Count > 0)
        {
            var result = new Dictionary<string, decimal>(snapshotPrices, StringComparer.OrdinalIgnoreCase);
            foreach (var sym in symbols.Where(s => !result.ContainsKey(s)))
            {
                result[sym] = 0m;
            }
            return result;
        }

        // 3. No data → 0 for all symbols
        logger.LogWarning("No live or snapshot prices for {Symbols}", string.Join(",", symbols));
        return symbols.ToDictionary(s => s, _ => 0m, StringComparer.OrdinalIgnoreCase);
    }

    public (decimal TotalInvested, decimal CurrentValue, decimal GainLoss, decimal GainLossPercentage)
        CalculateInvestmentMetrics(decimal quantity, decimal purchasePrice, decimal currentPrice)
    {
        var totalInvested = quantity * purchasePrice;
        var currentValue  = quantity * currentPrice;
        var gainLoss      = currentValue - totalInvested;
        var gainLossPercentage = totalInvested != 0
            ? Math.Round((gainLoss / totalInvested) * 100, 2)
            : 0m;
        return (totalInvested, currentValue, gainLoss, gainLossPercentage);
    }

    private async Task<Dictionary<string, decimal>> GetSnapshotPricesAsync(
        List<string> symbols,
        string targetCurrency,
        CancellationToken cancellationToken)
    {
        if (symbols.Count == 0)
        {
            return [];
        }

        var latestDates = db.PriceSnapshots
            .Where(p => symbols.Contains(p.Symbol))
            .GroupBy(p => p.Symbol)
            .Select(g => new { Symbol = g.Key, MaxDate = g.Max(p => p.Date) });

        var snapshotPricesUsd = await db.PriceSnapshots
            .Join(latestDates,
                p   => new { p.Symbol, p.Date },
                sub => new { sub.Symbol, Date = sub.MaxDate },
                (p, _) => p)
            .ToDictionaryAsync(p => p.Symbol, p => p.PriceUsd, cancellationToken);

        if (snapshotPricesUsd.Count == 0)
        {
            return [];
        }

        var usdToTarget = 1m;
        if (!string.Equals(targetCurrency, "USD", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                usdToTarget = await currencyConversion.GetExchangeRateAsync(
                    "USD", targetCurrency, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not fetch USD→{Currency} rate for snapshot prices", targetCurrency);
            }
        }

        return snapshotPricesUsd.ToDictionary(
            kv => kv.Key,
            kv => Math.Round(kv.Value * usdToTarget, 8),
            StringComparer.OrdinalIgnoreCase);
    }
}
