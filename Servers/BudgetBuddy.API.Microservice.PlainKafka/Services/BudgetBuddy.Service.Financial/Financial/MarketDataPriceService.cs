using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace BudgetBuddy.Service.Financial.Financial;

/// <summary>
/// Orchestrates price lookups across registered <see cref="IPriceProvider"/> implementations.
/// Handles caching and routing — providers only deal with raw API calls.
/// </summary>
public class MarketDataPriceService(
    IEnumerable<IPriceProvider> providers,
    HybridCache cache,
    IOptions<PriceServiceSettings> settings,
    ILogger<MarketDataPriceService> logger) : IPriceService
{
    private readonly PriceServiceSettings _settings = settings.Value;

    public async Task<decimal> GetCurrentPriceAsync(
        string symbol,
        InvestmentType investmentType,
        string currencyCode = "USD",
        CancellationToken cancellationToken = default)
    {
        var upperSymbol   = symbol.ToUpperInvariant();
        var upperCurrency = currencyCode.ToUpperInvariant();

        return await cache.GetOrCreateAsync(
            $"price:{upperSymbol}:{upperCurrency}",
            async ct =>
            {
                var provider = ResolveProvider(investmentType);
                var prices = await provider.GetPricesAsync([upperSymbol], upperCurrency, ct);
                var p = prices.GetValueOrDefault(upperSymbol);
                if (p <= 0)
                {
                    throw new InvalidOperationException($"Provider returned no valid price for {upperSymbol}");
                }

                return p;
            },
            new HybridCacheEntryOptions
            {
                Expiration      = TimeSpan.FromMinutes(_settings.CacheDurationMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(_settings.CacheDurationMinutes / 3)
            },
            cancellationToken: cancellationToken);
    }

    public async Task<Dictionary<string, decimal>> GetBatchPricesAsync(
        List<(string Symbol, InvestmentType Type)> investments,
        string currencyCode = "USD",
        CancellationToken cancellationToken = default)
    {
        var upperCurrency = currencyCode.ToUpperInvariant();
        var cacheOptions = new HybridCacheEntryOptions
        {
            Expiration           = TimeSpan.FromMinutes(_settings.CacheDurationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(_settings.CacheDurationMinutes / 3)
        };

        var symbolTasks = investments
            .Select(i => (Symbol: i.Symbol.ToUpperInvariant(), i.Type))
            .DistinctBy(i => i.Symbol)
            .Select(async item =>
            {
                var provider = ResolveProvider(item.Type);
                decimal price;
                try
                {
                    price = await cache.GetOrCreateAsync(
                        $"price:{item.Symbol}:{upperCurrency}",
                        async ct =>
                        {
                            var prices = await provider.GetPricesAsync([item.Symbol], upperCurrency, ct);
                            var p = prices.GetValueOrDefault(item.Symbol);
                            if (p <= 0)
                            {
                                throw new InvalidOperationException($"Provider returned no valid price for {item.Symbol}");
                            }

                            return p;
                        },
                        cacheOptions,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Price provider {Provider} failed for symbol: {Symbol}",
                        provider.GetType().Name, item.Symbol);
                    price = 0m;
                }

                return (item.Symbol, price);
            });

        var results = await Task.WhenAll(symbolTasks);

        return results
            .Where(r => r.price > 0)
            .ToDictionary(r => r.Symbol, r => r.price, StringComparer.OrdinalIgnoreCase);
    }

    private IPriceProvider ResolveProvider(InvestmentType type)
    {
        var provider = providers.FirstOrDefault(p => p.SupportedTypes.Contains(type));
        if (provider is null)
        {
            throw new InvalidOperationException($"No IPriceProvider registered for InvestmentType.{type}.");
        }
        return provider;
    }
}
