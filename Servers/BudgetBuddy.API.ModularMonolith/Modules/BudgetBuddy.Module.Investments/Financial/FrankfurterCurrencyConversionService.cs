using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Contracts.Financial;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodaTime;

namespace BudgetBuddy.Module.Investments.Financial;

/// <summary>
/// Currency conversion using the Frankfurter API (frankfurter.app).
/// Free, no API key required, based on European Central Bank reference rates.
/// Rates are updated on ECB working days (Mon–Fri), typically around 16:00 CET.
/// </summary>
public class FrankfurterCurrencyConversionService(
    HttpClient httpClient,
    HybridCache cache,
    IOptions<ExchangeRateSettings> settings,
    ILogger<FrankfurterCurrencyConversionService> logger) : ICurrencyConversionService, IFxHistoricalProvider
{
    private readonly ExchangeRateSettings _settings = settings.Value;

    // Currencies supported by the Frankfurter API (ECB subset)
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "AUD", "BGN", "BRL", "CAD", "CHF", "CNY", "CZK", "DKK", "EUR", "GBP",
        "HKD", "HUF", "IDR", "ILS", "INR", "ISK", "JPY", "KRW", "MXN", "MYR",
        "NOK", "NZD", "PHP", "PLN", "RON", "SEK", "SGD", "THB", "TRY", "USD", "ZAR"
    };

    public async Task<decimal> ConvertAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default)
    {
        if (fromCurrency.Equals(toCurrency, StringComparison.OrdinalIgnoreCase))
            return amount;

        var rate = await GetExchangeRateAsync(fromCurrency, toCurrency, cancellationToken);
        return Math.Round(amount * rate, 6);
    }

    public async Task<decimal> GetExchangeRateAsync(
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default)
    {
        var from = fromCurrency.ToUpperInvariant();
        var to = toCurrency.ToUpperInvariant();

        if (from == to) return 1m;

        var rates = await GetRatesAsync(from, cancellationToken);

        if (rates.TryGetValue(to, out var rate))
            return rate;

        logger.LogWarning("Exchange rate not found for {From}/{To}", from, to);
        return 1m;
    }

    public async Task<Dictionary<string, decimal>> GetRatesAsync(
        string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        var baseUpper = baseCurrency.ToUpperInvariant();

        return await cache.GetOrCreateAsync(
            $"exchange_rates:{baseUpper}",
            ct => FetchRatesFromApiAsync(baseUpper, ct),
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromHours(_settings.CacheDurationHours),
                LocalCacheExpiration = TimeSpan.FromMinutes(30)
            },
            cancellationToken: cancellationToken);
    }

    public Task<bool> IsSupportedAsync(string fromCurrency, string toCurrency) =>
        Task.FromResult(
            SupportedCurrencies.Contains(fromCurrency) &&
            SupportedCurrencies.Contains(toCurrency));

    private async ValueTask<Dictionary<string, decimal>> FetchRatesFromApiAsync(
        string baseCurrency, CancellationToken cancellationToken)
    {
        logger.LogDebug("Fetching exchange rates from Frankfurter API for base {Base}", baseCurrency);

        var response = await httpClient.GetFromJsonAsync<FrankfurterResponse>(
            $"/latest?from={baseCurrency}",
            cancellationToken);

        if (response?.Rates is null || response.Rates.Count == 0)
        {
            logger.LogWarning("Empty or null rates returned from Frankfurter API for base {Base}", baseCurrency);
            return new Dictionary<string, decimal> { [baseCurrency] = 1m };
        }

        var rates = new Dictionary<string, decimal>(response.Rates, StringComparer.OrdinalIgnoreCase)
        {
            [baseCurrency] = 1m
        };

        logger.LogDebug("Fetched {Count} exchange rates for base {Base} (date: {Date})",
            rates.Count, baseCurrency, response.Date);

        return rates;
    }

    public async Task<Dictionary<LocalDate, Dictionary<string, decimal>>> GetDailyRatesAsync(
        LocalDate from,
        LocalDate to,
        string baseCurrency = "USD",
        CancellationToken cancellationToken = default)
    {
        var baseUpper = baseCurrency.ToUpperInvariant();
        var fromStr = from.ToString("yyyy-MM-dd", null);
        var toStr = to.ToString("yyyy-MM-dd", null);

        logger.LogDebug("Fetching historical FX rates {From}..{To} base {Base}", fromStr, toStr, baseUpper);

        var response = await httpClient.GetFromJsonAsync<FrankfurterRangeResponse>(
            $"/{fromStr}..{toStr}?base={baseUpper}",
            cancellationToken);

        if (response?.Rates is null || response.Rates.Count == 0)
        {
            logger.LogWarning("Empty historical rates from Frankfurter for {From}..{To}", fromStr, toStr);
            return [];
        }

        var result = new Dictionary<LocalDate, Dictionary<string, decimal>>();
        foreach (var (dateStr, rates) in response.Rates)
        {
            if (!DateTime.TryParseExact(dateStr, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt))
                continue;
            var date = LocalDate.FromDateTime(dt);

            result[date] = new Dictionary<string, decimal>(rates, StringComparer.OrdinalIgnoreCase)
            {
                [baseUpper] = 1m
            };
        }

        return result;
    }

    private sealed record FrankfurterRangeResponse(
        [property: JsonPropertyName("base")] string Base,
        [property: JsonPropertyName("rates")] Dictionary<string, Dictionary<string, decimal>> Rates);

    private sealed record FrankfurterResponse(
        [property: JsonPropertyName("base")] string Base,
        [property: JsonPropertyName("date")] string Date,
        [property: JsonPropertyName("rates")] Dictionary<string, decimal> Rates);
}
