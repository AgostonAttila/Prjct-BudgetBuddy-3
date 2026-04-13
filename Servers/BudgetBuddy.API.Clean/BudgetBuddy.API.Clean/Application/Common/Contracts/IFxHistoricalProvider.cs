namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>
/// Provides historical daily FX rates for a date range.
/// </summary>
public interface IFxHistoricalProvider
{
    /// <summary>
    /// Returns daily rates for all supported currencies relative to <paramref name="baseCurrency"/>
    /// for each business day in [<paramref name="from"/>, <paramref name="to"/>].
    /// </summary>
    Task<Dictionary<LocalDate, Dictionary<string, decimal>>> GetDailyRatesAsync(
        LocalDate from,
        LocalDate to,
        string baseCurrency = "USD",
        CancellationToken cancellationToken = default);
}
