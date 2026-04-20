namespace BudgetBuddy.Module.Analytics.Features.Reports.GetMonthlySummary;

public class GetMonthlySummaryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/monthly-summary", async (
            IMediator mediator,
            int year,
            int month,
            Guid? accountId,
            string? displayCurrency,
            CancellationToken cancellationToken) =>
        {

            var query = new GetMonthlySummaryQuery(year, month, accountId, displayCurrency);
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get monthly summary report")
        .WithDescription("Generates a comprehensive financial summary for a specific month and year with optional account filtering. Amounts can be converted to a display currency using the displayCurrency parameter.")
        .WithReportRateLimit()
        .RequireAuthorization()
        .CacheOutput("monthly-summary")
        .WithTags("Reports")
        .WithName("GetMonthlySummary")
        ;
    }
}
