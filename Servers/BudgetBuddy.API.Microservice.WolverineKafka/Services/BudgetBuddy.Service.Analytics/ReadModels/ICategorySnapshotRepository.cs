namespace BudgetBuddy.Service.Analytics.ReadModels;

public interface ICategorySnapshotRepository
{
    Task<List<CategorySnapshot>> FindByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<List<CategorySnapshot>> FindByUserAsync(string userId, CancellationToken ct = default);
    Task UpsertAsync(CategorySnapshot snapshot, CancellationToken ct = default);
    Task DeleteAsync(Guid categoryId, CancellationToken ct = default);
}
