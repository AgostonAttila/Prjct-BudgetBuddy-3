using BudgetBuddy.Shared.Contracts.Financial;

namespace BudgetBuddy.Shared.Infrastructure.Financial;

/// <summary>
/// Base service for currency conversion with exchange rate caching and batching
/// </summary>
public abstract class CurrencyServiceBase(
    ICurrencyConversionService currencyConversionService,
    ILogger logger)
{
    // Cache exchange rates per target currency to avoid redundant API calls within same request
    private readonly Dictionary<string, Dictionary<string, decimal>> _exchangeRateCache = new();

    /// <summary>
    /// Get exchange rates for a collection of items with currency codes.
    /// Groups by unique currencies and fetches rates once per currency.
    /// </summary>
    protected async Task<Dictionary<string, decimal>> GetExchangeRatesFromCollectionAsync<T>(
        IEnumerable<T> data,
        Func<T, string> currencyCodeSelector,
        string displayCurrency,
        CancellationToken cancellationToken)
    {
        displayCurrency = displayCurrency.ToUpperInvariant();

        var uniqueCurrencies = data
            .Select(currencyCodeSelector)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.ToUpperInvariant())
            .Distinct()
            .ToList();

        if (uniqueCurrencies.Count == 0)
            return new Dictionary<string, decimal>();

        if (!_exchangeRateCache.TryGetValue(displayCurrency, out var cachedRates))
        {
            cachedRates = new Dictionary<string, decimal>();
            _exchangeRateCache[displayCurrency] = cachedRates;
        }

        var missingCurrencies = uniqueCurrencies
            .Where(c => !cachedRates.ContainsKey(c))
            .ToList();

        if (missingCurrencies.Count > 0)
        {
            var exchangeRateTasks = missingCurrencies.Select(async currencyCode =>
            {
                if (currencyCode == displayCurrency)
                    return (CurrencyCode: currencyCode, ExchangeRate: 1m);

                try
                {
                    var rate = await currencyConversionService.ConvertAsync(
                        1m,
                        currencyCode,
                        displayCurrency,
                        cancellationToken);
                    return (CurrencyCode: currencyCode, ExchangeRate: rate);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to get exchange rate from {From} to {To}, using 1.0",
                        currencyCode, displayCurrency);
                    return (CurrencyCode: currencyCode, ExchangeRate: 1m);
                }
            });

            var newRates = await Task.WhenAll(exchangeRateTasks);
            foreach (var (code, rate) in newRates)
                cachedRates[code] = rate;
        }

        return uniqueCurrencies.ToDictionary(c => c, c => cachedRates[c]);
    }

    /// <summary>
    /// Get exchange rates for a list of currency codes.
    /// Fetches rates once per unique currency.
    /// </summary>
    protected async Task<Dictionary<string, decimal>> GetExchangeRatesAsync(
        IEnumerable<string> currencyCodes,
        string displayCurrency,
        CancellationToken cancellationToken)
    {
        return await GetExchangeRatesFromCollectionAsync(
            currencyCodes.Select(c => new { CurrencyCode = c }),
            x => x.CurrencyCode,
            displayCurrency,
            cancellationToken);
    }

    /// <summary>
    /// Converts a single amount from one currency to another.
    /// Uses caching to avoid redundant API calls.
    /// </summary>
    protected async Task<decimal> ConvertAmountAsync(
        decimal amount,
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken)
    {
        fromCurrency = fromCurrency.ToUpperInvariant();
        toCurrency = toCurrency.ToUpperInvariant();

        if (fromCurrency == toCurrency)
            return amount;

        var rates = await GetExchangeRatesAsync([fromCurrency], toCurrency, cancellationToken);
        var rate = rates.GetValueOrDefault(fromCurrency, 1m);
        return Math.Round(amount * rate, 2);
    }

    /// <summary>
    /// Converts multiple amounts with their currency codes and sums them in the target currency.
    /// Optimized with batch exchange rate fetching.
    /// </summary>
    protected async Task<decimal> ConvertAndSumAsync<T>(
        IEnumerable<T> items,
        Func<T, decimal> amountSelector,
        Func<T, string> currencyCodeSelector,
        string targetCurrency,
        CancellationToken cancellationToken)
    {
        var itemsList = items.ToList();
        if (itemsList.Count == 0)
            return 0;

        var rates = await GetExchangeRatesFromCollectionAsync(
            itemsList,
            currencyCodeSelector,
            targetCurrency,
            cancellationToken);

        var total = itemsList.Sum(item =>
        {
            var amount = amountSelector(item);
            var currencyCode = currencyCodeSelector(item).ToUpperInvariant();
            var rate = rates.GetValueOrDefault(currencyCode, 1m);
            return amount * rate;
        });

        return Math.Round(total, 2);
    }
}
