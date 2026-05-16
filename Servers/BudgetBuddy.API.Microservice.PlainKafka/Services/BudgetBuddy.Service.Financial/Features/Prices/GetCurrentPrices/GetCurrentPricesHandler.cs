namespace BudgetBuddy.Service.Financial.Features.Prices.GetCurrentPrices;

public class GetCurrentPricesHandler(IPriceService priceService)
    : IRequestHandler<GetCurrentPricesQuery, Dictionary<string, decimal>>
{
    public Task<Dictionary<string, decimal>> Handle(
        GetCurrentPricesQuery request,
        CancellationToken cancellationToken)
        => priceService.GetBatchPricesAsync(request.SymbolsWithTypes, request.Currency, cancellationToken);
}
