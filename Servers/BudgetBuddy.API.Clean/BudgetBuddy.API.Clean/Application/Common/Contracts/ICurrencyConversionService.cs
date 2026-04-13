namespace BudgetBuddy.Application.Common.Contracts;

/// <summary>
/// Service for currency conversion using exchange rates
/// </summary>
public interface ICurrencyConversionService
{
    /// <summary>
    /// Convert amount from one currency to another
    /// </summary>
    /// <param name="amount">Amount to convert</param>
    /// <param name="fromCurrency">Source currency code (e.g., USD, EUR)</param>
    /// <param name="toCurrency">Target currency code (e.g., HUF, GBP)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Converted amount in target currency</returns>
    Task<decimal> ConvertAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get exchange rate between two currencies
    /// </summary>
    /// <param name="fromCurrency">Source currency code</param>
    /// <param name="toCurrency">Target currency code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exchange rate (1 fromCurrency = X toCurrency)</returns>
    Task<decimal> GetExchangeRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all exchange rates for a base currency
    /// </summary>
    /// <param name="baseCurrency">Base currency code (e.g., USD)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of currency codes and their rates relative to base</returns>
    Task<Dictionary<string, decimal>> GetRatesAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if conversion is supported between two currencies
    /// </summary>
    /// <param name="fromCurrency">Source currency</param>
    /// <param name="toCurrency">Target currency</param>
    /// <returns>True if conversion is supported</returns>
    Task<bool> IsSupportedAsync(string fromCurrency, string toCurrency);
}
