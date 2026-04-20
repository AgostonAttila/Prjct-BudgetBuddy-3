namespace BudgetBuddy.Module.Investments.Features.GetInvestments;

public class GetInvestmentsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/investments", async (
            IMediator mediator,
            InvestmentType? type,
            string? search,
            int pageNumber = 1,
            int pageSize = 50,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetInvestmentsQuery(type, search, pageNumber, pageSize);
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get all investments")
        .WithDescription("Retrieves all investments for the authenticated user with optional filtering by investment type, search query, and pagination.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .CacheOutput("investments")
        .WithTags("Investments")
        .WithName("GetInvestments")
        ;
    }
}
