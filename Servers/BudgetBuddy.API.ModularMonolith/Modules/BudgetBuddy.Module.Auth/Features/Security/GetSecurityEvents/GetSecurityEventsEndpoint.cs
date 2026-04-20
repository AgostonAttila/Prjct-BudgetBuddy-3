using BudgetBuddy.Shared.Kernel.Constants;
using BudgetBuddy.Shared.Kernel.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.Module.Auth.Features.Security.GetSecurityEvents;

public class GetSecurityEventsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/security/events", async (
            IMediator mediator,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] SecurityEventSeverity? minSeverity = null,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetSecurityEventsQuery(pageNumber, pageSize, minSeverity);
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithSummary("Get recent security events")
        .WithDescription("Retrieves recent security events for monitoring and auditing (Admin only)")
        .RequireAuthorization(AppPolicies.AdminOnly)  // Admin only!
        .WithStandardRateLimit()
        .WithTags("Security")
        .WithName("GetSecurityEvents");
    }
}
