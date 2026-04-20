using BudgetBuddy.Shared.Contracts.Financial;
using Microsoft.Extensions.Logging;

namespace BudgetBuddy.Module.Investments.Financial;

/// <summary>
/// Stub implementation of ICurrencyConversionService for development/testing.
/// Returns mock exchange rates.
/// </summary>
public class StubCurrencyConversionService : ICurrencyConversionService
{
    private readonly ILogger<StubCurrencyConversionService> _logger;

    private readonly Dictionary<string, decimal> _mockRates = new()
    {
        { "USD", 1.00m },
        { "EUR", 0.92m },
        { "GBP", 0.79m },
        { "HUF", 370.50m },
        { "JPY", 149.75m },
        { "CHF", 0.88m },
        { "CAD", 1.36m },
        { "AUD", 1.52m },
        { "CNY", 7.24m },
        { "SEK", 10.45m },
        { "NOK", 10.78m },
        { "DKK", 6.87m },
        { "PLN", 4.02m },
        { "CZK", 22.85m },
        { "RON", 4.58m }
    };

    public StubCurrencyConversionService(ILogger<StubCurrencyConversionService> logger)
    {
        _logger = logger;
    }

    public Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("STUB: Converting {Amount} from {From} to {To}", amount, fromCurrency, toCurrency);

        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(amount);

        var fromUpper = fromCurrency.ToUpperInvariant();
        var toUpper = toCurrency.ToUpperInvariant();

        if (!_mockRates.ContainsKey(fromUpper) || !_mockRates.ContainsKey(toUpper))
        {
            _logger.LogWarning("STUB: Unsupported currency pair {From}/{To}", fromCurrency, toCurrency);
            return Task.FromResult(amount);
        }

        var amountInUsd = amount / _mockRates[fromUpper];
        var convertedAmount = amountInUsd * _mockRates[toUpper];
        return Task.FromResult(Math.Round(convertedAmount, 2));
    }

    public Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency,
        CancellationToken cancellationToken = default)
    {
        var fromUpper = fromCurrency.ToUpperInvariant();
        var toUpper = toCurrency.ToUpperInvariant();

        if (fromUpper == toUpper) return Task.FromResult(1.0m);

        if (!_mockRates.ContainsKey(fromUpper) || !_mockRates.ContainsKey(toUpper))
            return Task.FromResult(1.0m);

        var rate = _mockRates[toUpper] / _mockRates[fromUpper];
        return Task.FromResult(Math.Round(rate, 4));
    }

    public Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        var baseUpper = baseCurrency.ToUpperInvariant();
        if (!_mockRates.ContainsKey(baseUpper))
            return Task.FromResult(_mockRates);

        var baseRate = _mockRates[baseUpper];
        var rates = _mockRates.ToDictionary(kvp => kvp.Key, kvp => Math.Round(kvp.Value / baseRate, 4));
        return Task.FromResult(rates);
    }

    public Task<bool> IsSupportedAsync(string fromCurrency, string toCurrency)
    {
        return Task.FromResult(
            _mockRates.ContainsKey(fromCurrency.ToUpperInvariant()) &&
            _mockRates.ContainsKey(toCurrency.ToUpperInvariant()));
    }
}
