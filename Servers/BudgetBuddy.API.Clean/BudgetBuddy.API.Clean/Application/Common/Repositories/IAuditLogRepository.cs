namespace BudgetBuddy.Application.Common.Repositories;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<List<AuditLog>> GetByEntityAsync(string entityName, string entityId, int limit = 100, CancellationToken ct = default);
}
