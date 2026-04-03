namespace BudgetBuddy.API.VSA.Features.Budgets.GetBudgets;

public class GetBudgetsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/budgets", async (
            IMediator mediator,
            int? year,
            int? month,
            Guid? categoryId,
            CancellationToken cancellationToken) =>
        {
            var query = new GetBudgetsQuery(year, month, categoryId);
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get all budgets")
        .WithDescription("Retrieves all budgets for the authenticated user with optional filtering by year, month, and category.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .CacheOutput("budgets-list")
        .WithTags("Budgets")
        .WithName("GetBudgets")
        ;
    }
}
