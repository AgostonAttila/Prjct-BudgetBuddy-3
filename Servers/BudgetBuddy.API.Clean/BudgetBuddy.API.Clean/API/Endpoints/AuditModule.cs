using BudgetBuddy.Application.Features.Audit.GetEntityAuditHistory;

namespace BudgetBuddy.API.Endpoints;

public class AuditModule : CarterModule
{
    public AuditModule() : base("/api/audit")
    {
        WithTags("Audit");
        RequireAuthorization(AppPolicies.AdminOnly);
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("{entityName}/{entityId}", GetEntityAuditHistory)
            .WithSummary("Get entity audit history")
            .WithDescription("Retrieves all changes (Create, Update, Delete) for a specific entity (Admin only)")
            .WithStandardRateLimit()
            .WithName("GetEntityAuditHistory");
    }

    private static async Task<IResult> GetEntityAuditHistory(string entityName, string entityId, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetEntityAuditHistoryQuery(entityName, entityId), cancellationToken);
        return Results.Ok(result);
    }
}
