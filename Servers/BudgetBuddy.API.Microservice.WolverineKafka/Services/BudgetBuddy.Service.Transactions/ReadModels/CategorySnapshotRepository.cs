using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Service.Transactions.ReadModels;

public class CategorySnapshotRepository(TransactionsDbContext context) : ICategorySnapshotRepository
{
    public Task<List<CategorySnapshot>> FindByIdsAsync(IEnumerable<Guid> categoryIds, CancellationToken ct)
    {
        var ids = categoryIds.ToList();
        return context.CategorySnapshots
            .Where(c => ids.Contains(c.CategoryId))
            .ToListAsync(ct);
    }

    public Task<List<CategorySnapshot>> FindByUserAsync(string userId, CancellationToken ct)
        => context.CategorySnapshots
            .Where(c => c.UserId == userId)
            .ToListAsync(ct);

    public async Task UpsertAsync(CategorySnapshot snapshot, CancellationToken ct)
    {
        var existing = await context.CategorySnapshots
            .FirstOrDefaultAsync(c => c.CategoryId == snapshot.CategoryId, ct);

        if (existing is null)
        {
            context.CategorySnapshots.Add(snapshot);
        }
        else
        {
            existing.Name     = snapshot.Name;
            existing.Icon     = snapshot.Icon;
            existing.SyncedAt = snapshot.SyncedAt;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid categoryId, CancellationToken ct)
    {
        var existing = await context.CategorySnapshots
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId, ct);

        if (existing is not null)
        {
            context.CategorySnapshots.Remove(existing);
            await context.SaveChangesAsync(ct);
        }
    }
}
