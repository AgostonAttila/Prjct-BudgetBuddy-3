using NodaTime;

namespace BudgetBuddy.Shared.Contracts.Financial;

/// <summary>
/// Provides historical daily FX rates for a date range.
/// </summary>
public interface IFxHistoricalProvider
{
    Task<Dictionary<LocalDate, Dictionary<string, decimal>>> GetDailyRatesAsync(
        LocalDate from,
        LocalDate to,
        string baseCurrency = "USD",
        CancellationToken cancellationToken = default);
}
