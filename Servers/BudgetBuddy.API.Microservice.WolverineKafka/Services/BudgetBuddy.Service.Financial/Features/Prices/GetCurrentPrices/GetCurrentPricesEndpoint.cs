using BudgetBuddy.Service.Financial.Features.Prices.GetCurrentPrices;

namespace BudgetBuddy.Service.Financial.Features.Prices;

public class GetCurrentPricesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/prices/current", async (
                string symbols,
                string currency,
                ISender sender,
                CancellationToken ct) =>
            {
                var parsed = ParseSymbols(symbols);
                if (parsed.Count == 0)
                {
                    return Results.BadRequest("No valid symbols provided. Format: BTC:Crypto,AAPL:Stock");
                }

                var prices = await sender.Send(new GetCurrentPricesQuery(parsed, currency.ToUpperInvariant()), ct);
                return Results.Ok(prices);
            })
            .AllowAnonymous()
            .WithName("GetCurrentPrices")
            .WithTags("Prices");
    }

    private static List<(string Symbol, InvestmentType Type)> ParseSymbols(string symbols)
    {
        var result = new List<(string, InvestmentType)>();

        foreach (var pair in symbols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = pair.Split(':', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            if (!Enum.TryParse<InvestmentType>(parts[1].Trim(), ignoreCase: true, out var type))
            {
                continue;
            }
            result.Add((parts[0].Trim().ToUpperInvariant(), type));
        }

        return result;
    }
}
