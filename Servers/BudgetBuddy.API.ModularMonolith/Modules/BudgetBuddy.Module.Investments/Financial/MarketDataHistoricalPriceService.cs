using BudgetBuddy.Module.Investments.Financial.Providers;
using BudgetBuddy.Shared.Contracts.Investments;
using BudgetBuddy.Shared.Kernel.Enums;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace BudgetBuddy.Module.Investments.Financial;

/// <summary>
/// Routes historical price requests to the correct <see cref="IHistoricalPriceProvider"/>
/// based on InvestmentType.
/// </summary>
public class MarketDataHistoricalPriceService(
    IEnumerable<IHistoricalPriceProvider> providers,
    ILogger<MarketDataHistoricalPriceService> logger) : IHistoricalPriceService
{
    public async Task<Dictionary<LocalDate, decimal>> GetDailyClosePricesAsync(
        string symbol,
        InvestmentType type,
        LocalDate from,
        CancellationToken cancellationToken = default)
    {
        var provider = providers.FirstOrDefault(p => p.SupportedTypes.Contains(type));

        if (provider is null)
        {
            logger.LogWarning(
                "No IHistoricalPriceProvider registered for InvestmentType.{Type}, skipping {Symbol}",
                type, symbol);
            return [];
        }

        return await provider.GetDailyClosePricesAsync(symbol, from, cancellationToken);
    }
}
