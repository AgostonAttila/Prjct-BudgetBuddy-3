namespace BudgetBuddy.Shared.Messages.Contracts.Financial;

/// <summary>
/// Service for currency conversion using exchange rates.
/// Implemented per-service by FrankfurterCurrencyConversionService — calls the external Frankfurter API directly.
/// Not service-to-service; every service that needs FX registers its own HTTP client.
/// </summary>
public interface ICurrencyConversionService
{
    Task<decimal> ConvertAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default);

    Task<decimal> GetExchangeRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, decimal>> GetRatesAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default);

    Task<bool> IsSupportedAsync(string fromCurrency, string toCurrency);
}
