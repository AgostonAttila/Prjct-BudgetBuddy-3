using BudgetBuddy.API.VSA.Common.Domain.Entities;

namespace BudgetBuddy.API.VSA.Features.Audit.GetEntityAuditHistory;

public record GetEntityAuditHistoryQuery(
    string EntityName,
    string EntityId
) : IRequest<List<AuditLogResponse>>;

public record AuditLogResponse(
    Guid Id,
    string EntityName,
    string EntityId,
    AuditOperation Operation,
    string? UserId,
    string? UserIdentifier,
    string? Changes,
    DateTime CreatedAt
);
