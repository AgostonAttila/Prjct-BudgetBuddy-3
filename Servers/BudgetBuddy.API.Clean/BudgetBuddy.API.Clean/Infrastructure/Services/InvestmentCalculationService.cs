using BudgetBuddy.Infrastructure.Financial;
using BudgetBuddy.Application.Common.Contracts;

namespace BudgetBuddy.Infrastructure.Services;

/// <summary>
/// Service for investment-related calculations and price fetching.
/// Centralises the full fallback chain: live IPriceService → PriceSnapshot → purchasePriceFallback.
/// </summary>
public class InvestmentCalculationService(
    AppDbContext context,
    IClock clock,
    ICurrencyConversionService currencyConversionService,
    IPriceService? priceService,
    ILogger<InvestmentCalculationService> logger) : IInvestmentCalculationService
{
    public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(
        List<(string Symbol, InvestmentType Type)> symbolsWithTypes,
        string targetCurrency,
        Dictionary<string, decimal>? purchasePriceFallback = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Live
        if (priceService != null)
        {
            try
            {
                var livePrices = await priceService.GetBatchPricesAsync(
                    symbolsWithTypes, targetCurrency, cancellationToken);

                if (livePrices.Count > 0)
                    return livePrices;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Live price fetch failed for {Count} symbols, trying snapshot",
                    symbolsWithTypes.Count);
            }
        }
        else
        {
            logger.LogWarning("IPriceService not available, trying snapshot");
        }

        // 2. Snapshot (last 7 days, always USD → converted to targetCurrency)
        var symbols = symbolsWithTypes.Select(s => s.Symbol).ToList();
        var snapshotPrices = await GetSnapshotPricesAsync(symbols, targetCurrency, cancellationToken);
        if (snapshotPrices.Count > 0)
            return snapshotPrices;

        // 3. Purchase price fallback
        logger.LogWarning("No snapshot prices found for {Symbols}, using purchase price fallback",
            string.Join(",", symbols));
        return purchasePriceFallback ?? new Dictionary<string, decimal>();
    }

    public (decimal TotalInvested, decimal CurrentValue, decimal GainLoss, decimal GainLossPercentage)
        CalculateInvestmentMetrics(decimal quantity, decimal purchasePrice, decimal currentPrice)
    {
        var totalInvested = quantity * purchasePrice;
        var currentValue = quantity * currentPrice;
        var gainLoss = currentValue - totalInvested;
        // gainLossPercentage uses unrounded values to avoid compounding rounding error
        var gainLossPercentage = totalInvested > 0
            ? Math.Round((gainLoss / totalInvested) * 100, 2)
            : 0;

        // Return unrounded intermediates — callers round at display time
        return (totalInvested, currentValue, gainLoss, gainLossPercentage);
    }

    /// <summary>
    /// Returns the most recent close price for each symbol from <see cref="PriceSnapshot"/>,
    /// converted from USD to <paramref name="targetCurrency"/>.
    /// Looks back up to 7 days to handle weekends and market holidays.
    /// </summary>
    private async Task<Dictionary<string, decimal>> GetSnapshotPricesAsync(
        List<string> symbols,
        string targetCurrency,
        CancellationToken cancellationToken)
    {
        var threshold = clock.GetCurrentInstant().InUtc().Date.PlusDays(-7);

        var recentSnapshots = await context.PriceSnapshots
            .Where(p => symbols.Contains(p.Symbol) && p.Date >= threshold)
            .OrderByDescending(p => p.Date)
            .ToListAsync(cancellationToken);

        if (recentSnapshots.Count == 0)
            return [];

        var latestBySymbol = recentSnapshots
            .GroupBy(p => p.Symbol, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ClosePrice, StringComparer.OrdinalIgnoreCase);

        // PriceSnapshots are always in USD — convert to targetCurrency
        var usdRate = await currencyConversionService.ConvertAsync(1m, "USD", targetCurrency, cancellationToken);

        return latestBySymbol.ToDictionary(
            kv => kv.Key,
            kv => Math.Round(kv.Value * usdRate, 2),
            StringComparer.OrdinalIgnoreCase);
    }
}
