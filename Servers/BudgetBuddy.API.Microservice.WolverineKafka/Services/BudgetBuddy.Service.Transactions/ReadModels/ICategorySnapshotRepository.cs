namespace BudgetBuddy.Service.Transactions.ReadModels;

public interface ICategorySnapshotRepository
{
    Task<List<CategorySnapshot>> FindByIdsAsync(IEnumerable<Guid> categoryIds, CancellationToken ct);
    Task<List<CategorySnapshot>> FindByUserAsync(string userId, CancellationToken ct);
    Task UpsertAsync(CategorySnapshot snapshot, CancellationToken ct);
    Task DeleteAsync(Guid categoryId, CancellationToken ct);
}
