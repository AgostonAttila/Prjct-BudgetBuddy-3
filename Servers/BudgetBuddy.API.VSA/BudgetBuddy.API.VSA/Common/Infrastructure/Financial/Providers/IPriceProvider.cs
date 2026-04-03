using BudgetBuddy.API.VSA.Common.Domain.Enums;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Financial.Providers;

/// <summary>
/// Fetches live market prices from an external data source.
/// Each provider declares which investment types it supports.
/// Register multiple providers in DI — MarketDataPriceService routes to the right one automatically.
/// </summary>
public interface IPriceProvider
{
    /// <summary>Investment types this provider can handle.</summary>
    IReadOnlySet<InvestmentType> SupportedTypes { get; }

    /// <summary>
    /// Fetches current prices for the given symbols in the requested currency.
    /// Returns only symbols for which a price was found (missing symbols are omitted).
    /// </summary>
    Task<Dictionary<string, decimal>> GetPricesAsync(
        IEnumerable<string> symbols,
        string currency,
        CancellationToken cancellationToken = default);
}
