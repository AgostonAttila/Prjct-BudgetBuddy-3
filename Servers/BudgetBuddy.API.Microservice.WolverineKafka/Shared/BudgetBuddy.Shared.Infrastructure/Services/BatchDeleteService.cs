using BudgetBuddy.Shared.Kernel.Contracts;

namespace BudgetBuddy.Shared.Infrastructure.Services;

public class BatchDeleteService(ILogger<BatchDeleteService> logger) : IBatchDeleteService
{
    public async Task<BatchDeleteResult> DeleteAsync<TEntity>(
        IQueryable<TEntity> source,
        List<Guid> ids,
        string userId,
        string entityName,
        CancellationToken cancellationToken = default)
        where TEntity : class, IUserOwnedEntity
    {
        logger.LogInformation(
            "Starting batch delete for {Count} {EntityName} for user {UserId}",
            ids.Count, entityName, userId);

        var existing = await source
            .Where(t => ids.Contains(t.Id) && t.UserId == userId)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        var notFound = ids.Except(existing).ToList();
        var errors = notFound
            .Select(id => $"{entityName} {id} not found or does not belong to user")
            .ToList();

        var successCount = 0;
        if (existing.Count > 0)
        {
            successCount = await source
                .Where(t => existing.Contains(t.Id) && t.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        logger.LogInformation(
            "Batch delete completed: {SuccessCount}/{TotalRequested} {EntityName} deleted",
            successCount, ids.Count, entityName);

        return new BatchDeleteResult(ids.Count, successCount, ids.Count - successCount, errors);
    }
}
