namespace BudgetBuddy.API.VSA.Features.Investments.GetPortfolioValue;

public class GetPortfolioValueEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/portfolio/value", async (
            IMediator mediator,
            string? currency = null,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetPortfolioValueQuery(currency);
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get portfolio value")
        .WithDescription("Calculates the total current value of the investment portfolio in the specified currency.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .CacheOutput("portfolio-value")
        .WithTags("Portfolio")
        .WithName("GetPortfolioValue")
        ;
    }
}
