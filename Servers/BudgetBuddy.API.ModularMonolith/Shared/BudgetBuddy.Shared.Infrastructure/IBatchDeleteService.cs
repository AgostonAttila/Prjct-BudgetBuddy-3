using BudgetBuddy.Shared.Kernel.Contracts;

namespace BudgetBuddy.Shared.Infrastructure;

public record BatchDeleteResult(
    int TotalRequested,
    int SuccessCount,
    int FailedCount,
    List<string> Errors);

public interface IBatchDeleteService
{
    Task<BatchDeleteResult> DeleteAsync<TEntity>(
        IQueryable<TEntity> source,
        List<Guid> ids,
        string userId,
        string entityName,
        CancellationToken cancellationToken = default)
        where TEntity : class, IUserOwnedEntity;
}
