using BudgetBuddy.API.VSA.Common.Domain.Enums;
using NodaTime;

namespace BudgetBuddy.API.VSA.Common.Infrastructure.Financial.Providers;

/// <summary>
/// Provides full daily close price history for a symbol from a given date to today.
/// </summary>
public interface IHistoricalPriceProvider
{
    IReadOnlySet<InvestmentType> SupportedTypes { get; }

    /// <summary>Returns daily close prices in USD from <paramref name="from"/> to today.</summary>
    Task<Dictionary<LocalDate, decimal>> GetDailyClosePricesAsync(
        string symbol,
        LocalDate from,
        CancellationToken cancellationToken = default);
}
