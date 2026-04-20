using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Module.Auth.Features.Audit.GetEntityAuditHistory;

public class GetEntityAuditHistoryHandler(AppDbContext context)
    : IRequestHandler<GetEntityAuditHistoryQuery, List<AuditLogResponse>>
{
    public async Task<List<AuditLogResponse>> Handle(
        GetEntityAuditHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var auditLogs = await context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityName == request.EntityName && a.EntityId == request.EntityId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(100) // Limit to last 100 changes
            .ToListAsync(cancellationToken);

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
