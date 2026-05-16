using BudgetBuddy.Shared.Kernel.Enums;
using NodaTime.Text;

namespace BudgetBuddy.Service.Financial.Features.Prices.GetHistoricalPrices;

public class GetHistoricalPricesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/prices/historical", async (
            string symbol,
            InvestmentType type,
            string from,
            ISender sender,
            CancellationToken ct) =>
        {
            var parseResult = LocalDatePattern.Iso.Parse(from);
            if (!parseResult.Success)
            {
                return Results.BadRequest("Invalid 'from' date. Expected ISO format: YYYY-MM-DD");
            }

            var query = new GetHistoricalPricesQuery(symbol.ToUpperInvariant(), type, parseResult.Value);
            var prices = await sender.Send(query, ct);
            return Results.Ok(prices);
        })
        .AllowAnonymous()
        .WithName("GetHistoricalPrices")
        .WithSummary("Get historical daily close prices")
        .WithDescription("Returns daily close prices in USD for a symbol from the given date to today. Example: ?symbol=AAPL&type=Stock&from=2023-01-01")
        .WithTags("Prices");
    }
}
