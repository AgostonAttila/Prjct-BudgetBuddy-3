using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Audit.GetEntityAuditHistory;

public class GetEntityAuditHistoryHandler(IAuditLogRepository auditLogRepo)
    : IRequestHandler<GetEntityAuditHistoryQuery, List<AuditLogResponse>>
{
    public async Task<List<AuditLogResponse>> Handle(
        GetEntityAuditHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var auditLogs = await auditLogRepo.GetByEntityAsync(
            request.EntityName, request.EntityId, limit: 100, ct: cancellationToken);

        return auditLogs.Select(a => new AuditLogResponse(
            a.Id,
            a.EntityName,
            a.EntityId,
            a.Operation,
            a.UserId,
            a.UserIdentifier,
            a.Changes,
            a.CreatedAt.ToDateTimeUtc()
        )).ToList();
    }
}
