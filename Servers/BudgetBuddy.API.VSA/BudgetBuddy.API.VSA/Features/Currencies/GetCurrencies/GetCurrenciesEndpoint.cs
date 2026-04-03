namespace BudgetBuddy.API.VSA.Features.Currencies.GetCurrencies;

public class GetCurrenciesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/currencies", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCurrenciesQuery();
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get all currencies")
        .WithDescription("Retrieves all currencies available to the authenticated user.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .CacheOutput("currencies")
        .WithTags("Currencies")
        .WithName("GetCurrencies")
        ;
    }
}
