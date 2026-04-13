using BudgetBuddy.Application.Features.BudgetAlerts.GetBudgetAlerts;

namespace BudgetBuddy.API.Endpoints;

public class BudgetAlertModule : CarterModule
{
    public BudgetAlertModule() : base("/api/budget-alerts")
    {
        WithTags("Budget Alerts");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", GetBudgetAlerts)
            .WithSummary("Get budget alerts")
            .WithDescription("Retrieves budget alerts for categories where spending is approaching or exceeding budgeted amounts for the specified period.")
            .CacheOutput("budget-alerts")
            .WithName("GetBudgetAlerts")
            .Produces<GetBudgetAlertsResponse>(200);
    }

    private static async Task<IResult> GetBudgetAlerts(int? year, int? month, ISender sender)
    {
        var result = await sender.Send(new GetBudgetAlertsQuery(year, month));
        return Results.Ok(result);
    }
}
