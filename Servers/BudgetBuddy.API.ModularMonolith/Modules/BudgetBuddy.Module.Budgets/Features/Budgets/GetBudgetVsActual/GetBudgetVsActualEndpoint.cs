namespace BudgetBuddy.Module.Budgets.Features.GetBudgetVsActual;

public class GetBudgetVsActualEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/budgets/vs-actual", async (
            IMediator mediator,
            int year,
            int month,
            CancellationToken cancellationToken) =>
        {
            if (year < 2000 || year > 2100)
                return Results.BadRequest("Invalid year");

            if (month < 1 || month > 12)
                return Results.BadRequest("Invalid month");

            var query = new GetBudgetVsActualQuery(year, month);
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get budget vs actual spending")
        .WithDescription("Compares budgeted amounts against actual spending for all categories in a specific month and year.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .CacheOutput("budget-vs-actual")
        .WithTags("Budgets")
        .WithName("GetBudgetVsActual")
        ;
    }
}
