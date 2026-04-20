using BudgetBuddy.Shared.Kernel.Entities;

namespace BudgetBuddy.Module.Auth.Features.Audit.GetEntityAuditHistory;

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
