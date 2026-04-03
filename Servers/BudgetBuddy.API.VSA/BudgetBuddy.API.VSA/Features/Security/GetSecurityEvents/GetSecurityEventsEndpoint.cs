using BudgetBuddy.API.VSA.Common.Domain.Constants;
using BudgetBuddy.API.VSA.Common.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.API.VSA.Features.Security.GetSecurityEvents;

public class GetSecurityEventsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/security/events", async (
            IMediator mediator,
            [FromQuery] int pageSize = 50,
            [FromQuery] SecurityEventSeverity? minSeverity = null,
            CancellationToken cancellationToken = default) =>
        {
            var query = new GetSecurityEventsQuery(pageSize, minSeverity);
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
