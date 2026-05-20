using BudgetBuddy.Service.Analytics.Persistence;

namespace BudgetBuddy.Service.Analytics.ReadModels;

public class CategorySnapshotRepository(AnalyticsDbContext context) : ICategorySnapshotRepository
{
    public Task<List<CategorySnapshot>> FindByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return context.CategorySnapshots.Where(c => idList.Contains(c.CategoryId)).ToListAsync(ct);
    }

    public Task<List<CategorySnapshot>> FindByUserAsync(string userId, CancellationToken ct = default)
        => context.CategorySnapshots.Where(c => c.UserId == userId).ToListAsync(ct);

    public async Task UpsertAsync(CategorySnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await context.CategorySnapshots.FindAsync([snapshot.CategoryId], ct);
        if (existing is null)
        {
            context.CategorySnapshots.Add(snapshot);
        }
        else
        {
            existing.UserId   = snapshot.UserId;
            existing.Name     = snapshot.Name;
            existing.Icon     = snapshot.Icon;
            existing.SyncedAt = snapshot.SyncedAt;
        }
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid categoryId, CancellationToken ct = default)
    {
        var existing = await context.CategorySnapshots.FindAsync([categoryId], ct);
        if (existing is not null)
        {
            context.CategorySnapshots.Remove(existing);
            await context.SaveChangesAsync(ct);
        }
    }
}
