using BudgetBuddy.Shared.Kernel.Enums;

namespace BudgetBuddy.Module.Investments.Financial.Providers;

/// <summary>
/// Fetches live market prices from an external data source.
/// Each provider declares which investment types it supports.
/// Register multiple providers in DI — MarketDataPriceService routes to the right one automatically.
/// </summary>
public interface IPriceProvider
{
    /// <summary>Investment types this provider can handle.</summary>
    IReadOnlySet<InvestmentType> SupportedTypes { get; }

    Task<Dictionary<string, decimal>> GetPricesAsync(
        IEnumerable<string> symbols,
        string currency,
        CancellationToken cancellationToken = default);
}
