using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Common.Infrastructure.Financial.Providers;
using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using NodaTime;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Financial;

/// <summary>
/// Routes historical price requests to the correct <see cref="IHistoricalPriceProvider"/>
/// based on InvestmentType. Mirrors the routing pattern of <see cref="MarketDataPriceService"/>.
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
