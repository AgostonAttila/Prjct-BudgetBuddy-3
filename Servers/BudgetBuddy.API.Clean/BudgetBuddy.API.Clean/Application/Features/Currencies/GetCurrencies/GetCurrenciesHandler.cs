using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Currencies.GetCurrencies;

public class GetCurrenciesHandler(
    ICurrencyRepository currencyRepo,
    IAppCache appCache,
    ILogger<GetCurrenciesHandler> logger) : IRequestHandler<GetCurrenciesQuery, List<CurrencyDto>>
{
    private static readonly AppCacheOptions CacheOptions = new(
        Ttl: TimeSpan.FromHours(1),
        LocalTtl: TimeSpan.FromMinutes(15));

    public async Task<List<CurrencyDto>> Handle(
        GetCurrenciesQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching global currencies");

        var currencies = await appCache.GetOrCreateAsync(
            "currencies:global-list",
            async cancel =>
            {
                var all = await currencyRepo.GetAllOrderedAsync(cancel);
                return all.Select(c => new CurrencyDto(c.Id, c.Code, c.Symbol, c.Name)).ToList();
            },
            CacheOptions,
            ct: cancellationToken);

        logger.LogInformation("Found {Count} currencies", currencies.Count);

        return currencies;
    }
}
