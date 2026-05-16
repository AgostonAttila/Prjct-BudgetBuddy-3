using Microsoft.Extensions.Caching.Hybrid;

namespace BudgetBuddy.Service.ReferenceData.Features.Currencies.GetCurrencies;

public class GetCurrenciesHandler(
    ReferenceDataDbContext context,
    HybridCache hybridCache,
    ILogger<GetCurrenciesHandler> logger) : IRequestHandler<GetCurrenciesQuery, List<CurrencyDto>>
{
    private static readonly Func<ReferenceDataDbContext, IAsyncEnumerable<CurrencyDto>> GetAllQuery =
        EF.CompileAsyncQuery((ReferenceDataDbContext ctx) =>
            ctx.Currencies
                .AsNoTracking()
                .OrderBy(c => c.Code)
                .Select(c => new CurrencyDto(c.Id, c.Code, c.Symbol, c.Name)));

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
                var result = new List<CurrencyDto>();
                await foreach (var dto in GetAllQuery(context).WithCancellation(cancel))
                {
                    result.Add(dto);
                }

                return result;
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
