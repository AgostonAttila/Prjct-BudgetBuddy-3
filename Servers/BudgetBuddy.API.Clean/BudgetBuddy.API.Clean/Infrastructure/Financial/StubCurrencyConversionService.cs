namespace BudgetBuddy.Infrastructure.Financial;

/// <summary>
/// Stub implementation of ICurrencyConversionService for development/testing
/// Returns mock exchange rates
/// TODO: Replace with real implementation (exchangerate-api.com, fixer.io, etc.)
/// </summary>
public class StubCurrencyConversionService : ICurrencyConversionService
{
    private readonly ILogger<StubCurrencyConversionService> _logger;

    // Mock exchange rates relative to USD (1 USD = X currency)
    private readonly Dictionary<string, decimal> _mockRates = new()
    {
        { "USD", 1.00m },       // US Dollar (base)
        { "EUR", 0.92m },       // Euro
        { "GBP", 0.79m },       // British Pound
        { "HUF", 370.50m },     // Hungarian Forint
        { "JPY", 149.75m },     // Japanese Yen
        { "CHF", 0.88m },       // Swiss Franc
        { "CAD", 1.36m },       // Canadian Dollar
        { "AUD", 1.52m },       // Australian Dollar
        { "CNY", 7.24m },       // Chinese Yuan
        { "SEK", 10.45m },      // Swedish Krona
        { "NOK", 10.78m },      // Norwegian Krone
        { "DKK", 6.87m },       // Danish Krone
        { "PLN", 4.02m },       // Polish Zloty
        { "CZK", 22.85m },      // Czech Koruna
        { "RON", 4.58m }        // Romanian Leu
    };

    public StubCurrencyConversionService(ILogger<StubCurrencyConversionService> logger)
    {
        _logger = logger;
    }

    public Task<decimal> ConvertAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("STUB: Converting {Amount} from {From} to {To}",
            amount, fromCurrency, toCurrency);

        // Same currency - no conversion needed
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(amount);
        }

        var fromUpper = fromCurrency.ToUpperInvariant();
        var toUpper = toCurrency.ToUpperInvariant();

        // Check if currencies are supported
        if (!_mockRates.ContainsKey(fromUpper) || !_mockRates.ContainsKey(toUpper))
        {
            _logger.LogWarning("STUB: Unsupported currency pair {From}/{To}, returning original amount",
                fromCurrency, toCurrency);
            return Task.FromResult(amount);
        }

        // Convert: amount (FROM) -> USD -> TO
        // Example: 100 EUR -> USD: 100 / 0.92 = 108.70 USD
        //          108.70 USD -> HUF: 108.70 * 370.50 = 40,273.35 HUF
        var amountInUsd = amount / _mockRates[fromUpper];
        var convertedAmount = amountInUsd * _mockRates[toUpper];

        _logger.LogDebug("STUB: Converted {Amount} {From} = {Result} {To} (rate: {Rate})",
            amount, fromCurrency, Math.Round(convertedAmount, 2), toCurrency,
            Math.Round(_mockRates[toUpper] / _mockRates[fromUpper], 4));

        return Task.FromResult(Math.Round(convertedAmount, 2));
    }

    public Task<decimal> GetExchangeRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("STUB: Getting exchange rate from {From} to {To}",
            fromCurrency, toCurrency);

        var fromUpper = fromCurrency.ToUpperInvariant();
        var toUpper = toCurrency.ToUpperInvariant();

        // Same currency
        if (fromUpper == toUpper)
        {
            return Task.FromResult(1.0m);
        }

        // Check if currencies are supported
        if (!_mockRates.ContainsKey(fromUpper) || !_mockRates.ContainsKey(toUpper))
        {
            _logger.LogWarning("STUB: Unsupported currency pair {From}/{To}, returning 1.0",
                fromCurrency, toCurrency);
            return Task.FromResult(1.0m);
        }

        // Calculate rate: 1 FROM = X TO
        var rate = _mockRates[toUpper] / _mockRates[fromUpper];

        _logger.LogDebug("STUB: 1 {From} = {Rate} {To}", fromCurrency, Math.Round(rate, 4), toCurrency);

        return Task.FromResult(Math.Round(rate, 4));
    }

    public Task<Dictionary<string, decimal>> GetRatesAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("STUB: Getting all rates for base currency {Base}", baseCurrency);

        var baseUpper = baseCurrency.ToUpperInvariant();

        if (!_mockRates.ContainsKey(baseUpper))
        {
            _logger.LogWarning("STUB: Unsupported base currency {Base}, returning USD rates", baseCurrency);
            return Task.FromResult(_mockRates);
        }

        // Calculate all rates relative to base currency
        var baseRate = _mockRates[baseUpper];
        var rates = _mockRates.ToDictionary(
            kvp => kvp.Key,
            kvp => Math.Round(kvp.Value / baseRate, 4)
        );

        _logger.LogDebug("STUB: Returning {Count} rates for {Base}", rates.Count, baseCurrency);

        return Task.FromResult(rates);
    }

    public Task<bool> IsSupportedAsync(string fromCurrency, string toCurrency)
    {
        var fromUpper = fromCurrency.ToUpperInvariant();
        var toUpper = toCurrency.ToUpperInvariant();

        var isSupported = _mockRates.ContainsKey(fromUpper) && _mockRates.ContainsKey(toUpper);

        _logger.LogDebug("STUB: Currency pair {From}/{To} supported: {IsSupported}",
            fromCurrency, toCurrency, isSupported);

        return Task.FromResult(isSupported);
    }
}
