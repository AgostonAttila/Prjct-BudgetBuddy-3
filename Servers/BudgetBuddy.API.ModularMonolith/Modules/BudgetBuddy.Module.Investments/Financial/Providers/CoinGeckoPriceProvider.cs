using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Kernel.Enums;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace BudgetBuddy.Module.Investments.Financial.Providers;

/// <summary>
/// Fetches cryptocurrency prices from the CoinGecko API (api.coingecko.com).
/// Free tier: ~30 req/min without a key.
/// </summary>
public class CoinGeckoPriceProvider(
    HttpClient httpClient,
    ILogger<CoinGeckoPriceProvider> logger) : IPriceProvider, IHistoricalPriceProvider
{
    public IReadOnlySet<InvestmentType> SupportedTypes { get; } =
        new HashSet<InvestmentType> { InvestmentType.Crypto };

    private static readonly Dictionary<string, string> CoinIds =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "BTC",   "bitcoin" },
            { "ETH",   "ethereum" },
            { "USDT",  "tether" },
            { "BNB",   "binancecoin" },
            { "SOL",   "solana" },
            { "XRP",   "ripple" },
            { "ADA",   "cardano" },
            { "DOGE",  "dogecoin" },
            { "AVAX",  "avalanche-2" },
            { "DOT",   "polkadot" },
            { "LINK",  "chainlink" },
            { "MATIC", "matic-network" },
            { "UNI",   "uniswap" },
            { "LTC",   "litecoin" },
            { "BCH",   "bitcoin-cash" },
            { "ATOM",  "cosmos" },
            { "XLM",   "stellar" },
            { "ALGO",  "algorand" },
        };

    public async Task<Dictionary<string, decimal>> GetPricesAsync(
        IEnumerable<string> symbols,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var symbolList = symbols.Select(s => s.ToUpperInvariant()).ToList();
        var currencyLower = currency.ToLowerInvariant();

        var symbolToId = symbolList.ToDictionary(
            s => s,
            s => CoinIds.GetValueOrDefault(s, s.ToLowerInvariant()));

        var idsParam = string.Join(",", symbolToId.Values.Distinct());
        var url = $"simple/price?ids={idsParam}&vs_currencies={currencyLower}";

        logger.LogDebug("CoinGecko: fetching prices for {Symbols} in {Currency}",
            string.Join(",", symbolList), currency);

        var response = await httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(
            url, cancellationToken);

        if (response is null)
        {
            logger.LogWarning("CoinGecko: empty response for {Symbols}", string.Join(",", symbolList));
            return [];
        }

        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var (symbol, coinId) in symbolToId)
        {
            if (response.TryGetValue(coinId, out var priceMap) &&
                priceMap.TryGetValue(currencyLower, out var price))
            {
                result[symbol] = price;
            }
            else
            {
                logger.LogWarning("CoinGecko: no price for {Symbol} (id: {Id}) in {Currency}",
                    symbol, coinId, currency);
            }
        }

        return result;
    }

    public async Task<Dictionary<LocalDate, decimal>> GetDailyClosePricesAsync(
        string symbol,
        LocalDate from,
        CancellationToken cancellationToken = default)
    {
        var upper = symbol.ToUpperInvariant();
        var coinId = CoinIds.GetValueOrDefault(upper, upper.ToLowerInvariant());

        var fromUnix = new DateTimeOffset(from.ToDateTimeUnspecified(), TimeSpan.Zero).ToUnixTimeSeconds();
        var toUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var url = $"coins/{coinId}/market_chart/range?vs_currency=usd&from={fromUnix}&to={toUnix}";

        logger.LogDebug("CoinGecko historical: {Symbol} (id: {Id}) from {From}", upper, coinId, from);

        var response = await httpClient.GetFromJsonAsync<CoinGeckoChartResponse>(url, cancellationToken);

        if (response?.Prices is null || response.Prices.Count == 0)
        {
            logger.LogWarning("CoinGecko historical: empty response for {Symbol}", upper);
            return [];
        }

        var prices = new Dictionary<LocalDate, decimal>();
        foreach (var point in response.Prices)
        {
            if (point.Count < 2) continue;
            var date = LocalDate.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds((long)point[0]).UtcDateTime);
            prices[date] = point[1];
        }

        return prices;
    }

    private sealed record CoinGeckoChartResponse(
        [property: JsonPropertyName("prices")] List<List<decimal>>? Prices);
}
