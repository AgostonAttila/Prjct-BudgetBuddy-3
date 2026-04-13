using BudgetBuddy.Domain.Enums;
using BudgetBuddy.Infrastructure.Financial.Providers;
using BudgetBuddy.Application.Common.Contracts;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace BudgetBuddy.Infrastructure.Financial;

/// <summary>
/// Orchestrates price lookups across registered <see cref="IPriceProvider"/> implementations.
/// Handles caching and routing — providers only deal with raw API calls.
/// Add new asset types by registering a new IPriceProvider in DI.
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
        var upperSymbol = symbol.ToUpperInvariant();
        var upperCurrency = currencyCode.ToUpperInvariant();

        return await cache.GetOrCreateAsync(
            $"price:{upperSymbol}:{upperCurrency}",
            async ct =>
            {
                var provider = ResolveProvider(investmentType);
                var prices = await provider.GetPricesAsync([upperSymbol], upperCurrency, ct);
                return prices.GetValueOrDefault(upperSymbol, 0m);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(_settings.CacheDurationMinutes),
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

        // Group by provider so each provider gets one batched API call
        var groups = investments
            .Select(i => (Symbol: i.Symbol.ToUpperInvariant(), i.Type))
            .GroupBy(i => ResolveProvider(i.Type));

        var batchTasks = groups.Select(async group =>
        {
            var symbols = group.Select(i => i.Symbol).Distinct().ToList();
            var provider = group.Key;

            try
            {
                var prices = await provider.GetPricesAsync(symbols, upperCurrency, cancellationToken);

                // Store each result individually in cache for future single-price lookups
                foreach (var (symbol, price) in prices)
                {
                    await cache.SetAsync(
                        $"price:{symbol}:{upperCurrency}",
                        price,
                        new HybridCacheEntryOptions
                        {
                            Expiration = TimeSpan.FromMinutes(_settings.CacheDurationMinutes),
                            LocalCacheExpiration = TimeSpan.FromMinutes(_settings.CacheDurationMinutes / 3)
                        },
                        cancellationToken: cancellationToken);
                }

                return prices;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Price provider {Provider} failed for symbols: {Symbols}",
                    provider.GetType().Name, string.Join(",", symbols));
                return new Dictionary<string, decimal>();
            }
        });

        var results = await Task.WhenAll(batchTasks);

        return results
            .SelectMany(r => r)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);
    }

    private IPriceProvider ResolveProvider(InvestmentType type)
    {
        var provider = providers.FirstOrDefault(p => p.SupportedTypes.Contains(type));

        if (provider is null)
            throw new InvalidOperationException(
                $"No IPriceProvider registered for InvestmentType.{type}. " +
                $"Register a provider that includes '{type}' in its SupportedTypes.");

        return provider;
    }
}
