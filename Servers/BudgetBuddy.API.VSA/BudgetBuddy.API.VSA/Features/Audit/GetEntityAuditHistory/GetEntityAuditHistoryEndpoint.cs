using BudgetBuddy.API.VSA.Common.Domain.Constants;

namespace BudgetBuddy.API.VSA.Features.Audit.GetEntityAuditHistory;

public class GetEntityAuditHistoryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/audit/{entityName}/{entityId}", async (
            string entityName,
            string entityId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetEntityAuditHistoryQuery(entityName, entityId);
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithSummary("Get entity audit history")
        .WithDescription("Retrieves all changes (Create, Update, Delete) for a specific entity (Admin only)")
        .RequireAuthorization(AppPolicies.AdminOnly)  // Admin only!
        .WithStandardRateLimit()
        .WithTags("Audit")
        .WithName("GetEntityAuditHistory");
    }
}
