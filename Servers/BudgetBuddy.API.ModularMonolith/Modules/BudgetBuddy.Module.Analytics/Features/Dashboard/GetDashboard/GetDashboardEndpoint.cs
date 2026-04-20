namespace BudgetBuddy.Module.Analytics.Features.Dashboard.GetDashboard;

public class GetDashboardEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", async (
            ISender sender,
            string? displayCurrency = null) =>  // Optional: Currency for displaying converted amounts
        {
            var query = new GetDashboardQuery(displayCurrency);
            var result = await sender.Send(query);

            return Results.Ok(result);
        })
        .WithSummary("Get dashboard data")
        .WithDescription("""
            Retrieves comprehensive dashboard overview including account balances, recent transactions, budget status, and financial summaries.

            **Query Parameters:**
            - displayCurrency (optional): Currency code (e.g., USD, EUR, HUF) for displaying all amounts. Defaults to user's default currency.

            **Note:** Currency conversion currently uses stub exchange rates. Configure ICurrencyConversionService for real-time rates.
            """)
        .WithApiRateLimit()
        .RequireAuthorization()
        .CacheOutput("dashboard")
        .WithTags("Dashboard")
        .WithName("GetDashboard")
        .Produces<GetDashboardResponse>(200)
        ;
    }
}
