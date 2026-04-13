using BudgetBuddy.Application.Features.Security.GetSecurityAlerts;
using BudgetBuddy.Application.Features.Security.GetSecurityEvents;
using BudgetBuddy.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.API.Endpoints;

public class SecurityModule : CarterModule
{
    public SecurityModule() : base("/api/security")
    {
        WithTags("Security");
        RequireAuthorization(AppPolicies.AdminOnly);
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("alerts", GetSecurityAlerts)
            .WithSummary("Get active security alerts")
            .WithDescription("Retrieves unreviewed high/critical severity security events (Admin only)")
            .WithStandardRateLimit()
            .WithName("GetSecurityAlerts");

        app.MapGet("events", GetSecurityEvents)
            .WithSummary("Get recent security events")
            .WithDescription("Retrieves recent security events for monitoring and auditing (Admin only)")
            .WithStandardRateLimit()
            .WithName("GetSecurityEvents");
    }

    private static async Task<IResult> GetSecurityAlerts(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSecurityAlertsQuery(), cancellationToken);
        return Results.Ok(new { alerts = result, count = result.Count });
    }

    private static async Task<IResult> GetSecurityEvents(
        IMediator mediator, [FromQuery] int pageSize = 50, [FromQuery] SecurityEventSeverity? minSeverity = null, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetSecurityEventsQuery(pageSize, minSeverity), cancellationToken);
        return Results.Ok(result);
    }
}
