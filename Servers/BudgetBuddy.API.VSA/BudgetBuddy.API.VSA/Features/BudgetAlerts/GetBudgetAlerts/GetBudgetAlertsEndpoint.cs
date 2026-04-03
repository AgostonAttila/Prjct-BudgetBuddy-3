
namespace BudgetBuddy.API.VSA.Features.BudgetAlerts.GetBudgetAlerts;

public class GetBudgetAlertsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/budget-alerts", async (
            int? year,
            int? month,
            ISender sender) =>
        {
            var query = new GetBudgetAlertsQuery(year, month);
            var result = await sender.Send(query);

            return Results.Ok(result);
        })
        .WithSummary("Get budget alerts")
        .WithDescription("Retrieves budget alerts for categories where spending is approaching or exceeding budgeted amounts for the specified period.")
        .RequireAuthorization()
        .CacheOutput("budget-alerts")
        .WithTags("Budget Alerts")
        .WithName("GetBudgetAlerts")
        .Produces<GetBudgetAlertsResponse>(200)
        ;
    }
}
