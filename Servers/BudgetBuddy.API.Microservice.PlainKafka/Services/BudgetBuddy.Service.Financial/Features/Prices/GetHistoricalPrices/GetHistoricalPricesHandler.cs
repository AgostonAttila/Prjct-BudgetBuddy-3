using BudgetBuddy.Service.Financial.Financial.Providers;

namespace BudgetBuddy.Service.Financial.Features.Prices.GetHistoricalPrices;

public class GetHistoricalPricesHandler(
    IEnumerable<IHistoricalPriceProvider> providers,
    ILogger<GetHistoricalPricesHandler> logger)
    : IRequestHandler<GetHistoricalPricesQuery, Dictionary<string, decimal>>
{
    public async Task<Dictionary<string, decimal>> Handle(
        GetHistoricalPricesQuery request,
        CancellationToken cancellationToken)
    {
        var provider = providers.FirstOrDefault(p => p.SupportedTypes.Contains(request.Type));

        if (provider is null)
        {
            logger.LogWarning(
                "No IHistoricalPriceProvider registered for InvestmentType.{Type}, symbol: {Symbol}",
                request.Type, request.Symbol);
            return [];
        }

        var prices = await provider.GetDailyClosePricesAsync(
            request.Symbol, request.From, cancellationToken);

        // ISO date string keys for HTTP transport — client parses back to LocalDate
        return prices.ToDictionary(
            kv => kv.Key.ToString(),
            kv => kv.Value);
    }
}
