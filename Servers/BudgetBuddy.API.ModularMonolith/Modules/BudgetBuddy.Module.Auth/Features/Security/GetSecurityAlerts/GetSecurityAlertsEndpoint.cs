using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.Auth.Features.Security.GetSecurityAlerts;

public class GetSecurityAlertsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/security/alerts", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetSecurityAlertsQuery();
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(new { alerts = result, count = result.Count });
        })
        .WithSummary("Get active security alerts")
        .WithDescription("Retrieves unreviewed high/critical severity security events (Admin only)")
        .RequireAuthorization(AppPolicies.AdminOnly)  // Admin only!
        .WithStandardRateLimit()
        .WithTags("Security")
        .WithName("GetSecurityAlerts");
    }
}
