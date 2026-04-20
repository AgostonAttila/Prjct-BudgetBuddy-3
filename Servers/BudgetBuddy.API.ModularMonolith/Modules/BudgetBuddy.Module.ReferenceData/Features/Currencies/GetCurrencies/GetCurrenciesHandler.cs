using Microsoft.Extensions.Caching.Hybrid;

namespace BudgetBuddy.Module.ReferenceData.Features.Currencies.GetCurrencies;

public class GetCurrenciesHandler(
    ReferenceDataDbContext context,
    HybridCache hybridCache,
    ILogger<GetCurrenciesHandler> logger) : IRequestHandler<GetCurrenciesQuery, List<CurrencyDto>>
{
    public async Task<List<CurrencyDto>> Handle(
        GetCurrenciesQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching global currencies");

        // Global reference data - perfect for aggressive caching
        const string cacheKey = "currencies:global-list";

        var currencies = await hybridCache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                return await context.Currencies
                    .AsNoTracking()
                    .OrderBy(c => c.Code)
                    .Select(c => new CurrencyDto(
                        c.Id,
                        c.Code,
                        c.Symbol,
                        c.Name
                    ))
                    .ToListAsync(cancel);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromHours(1),         // L2 cache: 1 hour (global data, rarely changes)
                LocalCacheExpiration = TimeSpan.FromMinutes(15) // L1 cache: 15 min
            },
            cancellationToken: cancellationToken
        );

        logger.LogInformation("Found {Count} currencies", currencies.Count);

        return currencies;
    }
}
