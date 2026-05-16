using BudgetBuddy.Shared.Messages.Contracts.ReferenceData;

namespace BudgetBuddy.Service.Transactions.ReadModels;

public class CategoryQueryService(ICategorySnapshotRepository repository) : ICategoryQueryService
{
    public async Task<Dictionary<Guid, CategoryInfo>> GetCategoriesByIdsAsync(
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await repository.FindByIdsAsync(categoryIds, cancellationToken);
        return snapshots.ToDictionary(
            s => s.CategoryId,
            s => new CategoryInfo(s.CategoryId, s.Name, s.Icon));
    }

    public async Task<Dictionary<string, Guid>> GetUserCategoryNameMapAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await repository.FindByUserAsync(userId, cancellationToken);
        return snapshots.ToDictionary(s => s.Name, s => s.CategoryId);
    }
}
