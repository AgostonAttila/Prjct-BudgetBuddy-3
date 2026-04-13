using BudgetBuddy.Application.Features.Dashboard.GetDashboard;

namespace BudgetBuddy.API.Endpoints;

public class DashboardModule : CarterModule
{
    public DashboardModule() : base("/api/dashboard")
    {
        WithTags("Dashboard");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", GetDashboard)
            .WithSummary("Get dashboard data")
            .WithDescription("Retrieves comprehensive dashboard overview including account balances, recent transactions, budget status, and financial summaries.")
            .WithApiRateLimit()
            .CacheOutput("dashboard")
            .WithName("GetDashboard")
            .Produces<GetDashboardResponse>(200);
    }

    private static async Task<IResult> GetDashboard(ISender sender, string? displayCurrency = null)
    {
        var result = await sender.Send(new GetDashboardQuery(displayCurrency));
        return Results.Ok(result);
    }
}
