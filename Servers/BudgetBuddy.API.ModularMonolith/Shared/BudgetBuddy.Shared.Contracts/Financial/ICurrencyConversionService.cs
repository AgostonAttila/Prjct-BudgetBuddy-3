namespace BudgetBuddy.Shared.Contracts.Financial;

/// <summary>
/// Service for currency conversion using exchange rates
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
