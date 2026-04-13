using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BudgetBuddy.Domain.Enums;
using BudgetBuddy.Application.Common.Contracts;
using Microsoft.Extensions.Caching.Hybrid;

namespace BudgetBuddy.Infrastructure.Financial.Providers;

/// <summary>
/// Fetches market prices using the Yahoo Finance unofficial v7 quote API.
/// Supports stocks, ETFs, mutual funds, bond ETFs, and REIT ETFs.
/// Requires crumb + cookie auth — crumb is fetched once and cached.
/// Note: Yahoo Finance does not provide an official public API — this endpoint is widely used
/// in personal finance applications but may change without notice.
/// </summary>
public class YahooFinancePriceProvider(
    HttpClient httpClient,
    ICurrencyConversionService currencyConversionService,
    HybridCache cache,
    ILogger<YahooFinancePriceProvider> logger) : IPriceProvider, IHistoricalPriceProvider
{
    private const string CrumbCacheKey = "yahoo:crumb";

    public IReadOnlySet<InvestmentType> SupportedTypes { get; } =
        new HashSet<InvestmentType>
        {
            InvestmentType.Stock,
            InvestmentType.Etf,
            InvestmentType.MutualFund,
            InvestmentType.Bond,
            InvestmentType.RealEstate,
        };

    public async Task<Dictionary<string, decimal>> GetPricesAsync(
        IEnumerable<string> symbols,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var symbolList = symbols.Select(s => s.ToUpperInvariant()).ToList();
        var symbolsParam = string.Join(",", symbolList);

        var crumb = await GetCrumbAsync(cancellationToken);
        var url = $"v7/finance/quote?symbols={symbolsParam}&crumb={Uri.EscapeDataString(crumb)}&lang=en-US&region=US";

        logger.LogDebug("Yahoo Finance: fetching prices for {Symbols} in {Currency}",
            symbolsParam, currency);

        var response = await httpClient.GetFromJsonAsync<YahooQuoteResponse>(url, cancellationToken);

        var quotes = response?.QuoteResponse?.Result;
        if (quotes is null || quotes.Count == 0)
        {
            logger.LogWarning("Yahoo Finance: empty response for {Symbols}", symbolsParam);
            return [];
        }

        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var quote in quotes)
        {
            if (quote.Symbol is null || quote.RegularMarketPrice is null)
                continue;

            var price = quote.RegularMarketPrice.Value;
            var nativeCurrency = quote.Currency ?? "USD";

            if (!nativeCurrency.Equals(currency, StringComparison.OrdinalIgnoreCase))
            {
                price = await currencyConversionService.ConvertAsync(
                    price, nativeCurrency, currency, cancellationToken);
            }

            result[quote.Symbol] = price;
        }

        return result;
    }

    // IHistoricalPriceProvider
    // Uses the v8/finance/chart endpoint which returns adjusted close prices.
    // Yahoo Finance returns data in the symbol's native currency (USD for US-listed stocks).
    // Note: the chart endpoint does not require a crumb — only the quote endpoint does.
    public async Task<Dictionary<LocalDate, decimal>> GetDailyClosePricesAsync(
        string symbol,
        LocalDate from,
        CancellationToken cancellationToken = default)
    {
        var upper = symbol.ToUpperInvariant();

        var period1 = new DateTimeOffset(from.ToDateTimeUnspecified(), TimeSpan.Zero).ToUnixTimeSeconds();
        var period2 = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var url = $"v8/finance/chart/{upper}?period1={period1}&period2={period2}&interval=1d";

        logger.LogDebug("Yahoo Finance historical: {Symbol} from {From}", upper, from);

        var response = await httpClient.GetFromJsonAsync<YahooChartResponse>(url, cancellationToken);

        var result = response?.Chart?.Result?.FirstOrDefault();
        if (result?.Timestamps is null)
        {
            logger.LogWarning("Yahoo Finance historical: empty response for {Symbol}", upper);
            return [];
        }

        // Prefer adjusted close (accounts for splits/dividends), fall back to regular close
        var closes = result.Indicators?.AdjClose?.FirstOrDefault()?.AdjClose
                     ?? result.Indicators?.Quote?.FirstOrDefault()?.Close;

        if (closes is null || closes.Count != result.Timestamps.Count)
        {
            logger.LogWarning("Yahoo Finance historical: price/timestamp mismatch for {Symbol}", upper);
            return [];
        }

        var prices = new Dictionary<LocalDate, decimal>();
        for (var i = 0; i < result.Timestamps.Count; i++)
        {
            if (closes[i] is null) continue;
            var date = LocalDate.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(result.Timestamps[i]).UtcDateTime);
            prices[date] = closes[i]!.Value;
        }

        return prices;
    }

    private async Task<string> GetCrumbAsync(CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(
            CrumbCacheKey,
            async ct =>
            {
                // Visit fc.yahoo.com to establish a session cookie
                await httpClient.GetAsync("https://fc.yahoo.com", ct);

                var crumb = await httpClient.GetStringAsync(
                    "https://query1.finance.yahoo.com/v1/test/getcrumb", ct);

                if (string.IsNullOrWhiteSpace(crumb))
                    throw new InvalidOperationException("Yahoo Finance returned empty crumb.");

                logger.LogDebug("Yahoo Finance: crumb acquired");
                return crumb;
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(24) },
            cancellationToken: cancellationToken);
    }

    private sealed record YahooChartResponse(
        [property: JsonPropertyName("chart")] YahooChartRoot? Chart);

    private sealed record YahooChartRoot(
        [property: JsonPropertyName("result")] List<YahooChartResult>? Result);

    private sealed record YahooChartResult(
        [property: JsonPropertyName("timestamp")] List<long>? Timestamps,
        [property: JsonPropertyName("indicators")] YahooChartIndicators? Indicators);

    private sealed record YahooChartIndicators(
        [property: JsonPropertyName("adjclose")] List<YahooAdjClose>? AdjClose,
        [property: JsonPropertyName("quote")] List<YahooQuoteData>? Quote);

    private sealed record YahooAdjClose(
        [property: JsonPropertyName("adjclose")] List<decimal?>? AdjClose);

    private sealed record YahooQuoteData(
        [property: JsonPropertyName("close")] List<decimal?>? Close);

    private sealed record YahooQuoteResponse(
        [property: JsonPropertyName("quoteResponse")] YahooQuoteResult? QuoteResponse);

    private sealed record YahooQuoteResult(
        [property: JsonPropertyName("result")] List<YahooQuote>? Result);

    private sealed record YahooQuote(
        [property: JsonPropertyName("symbol")] string? Symbol,
        [property: JsonPropertyName("regularMarketPrice")] decimal? RegularMarketPrice,
        [property: JsonPropertyName("currency")] string? Currency);
}
