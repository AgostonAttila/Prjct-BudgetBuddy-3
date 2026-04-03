namespace BudgetBuddy.API.VSA.Features.Categories.GetCategories;

public class GetCategoriesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categories", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCategoriesQuery();
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get all categories")
        .WithDescription("Retrieves all transaction categories for the authenticated user.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .CacheOutput("categories")
        .WithTags("Categories")
        .WithName("GetCategories")
        ;
    }
}
