using BudgetBuddy.Service.Analytics.Persistence;

namespace BudgetBuddy.Service.Analytics.ReadModels;

public class AccountSnapshotRepository(AnalyticsDbContext context) : IAccountSnapshotRepository
{
    public Task<AccountSnapshot?> FindAsync(Guid accountId, CancellationToken ct = default)
        => context.AccountSnapshots.FindAsync([accountId], ct).AsTask();

    public Task<List<AccountSnapshot>> FindByUserAsync(string userId, CancellationToken ct = default)
        => context.AccountSnapshots
            .Where(a => a.UserId == userId)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task UpsertAsync(AccountSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await context.AccountSnapshots.FindAsync([snapshot.AccountId], ct);
        if (existing is null)
        {
            context.AccountSnapshots.Add(snapshot);
        }
        else
        {
            existing.UserId   = snapshot.UserId;
            existing.Name     = snapshot.Name;
            existing.Currency = snapshot.Currency;
            existing.IsActive = snapshot.IsActive;
            existing.Balance  = snapshot.Balance;
            existing.Version  = snapshot.Version;
            existing.SyncedAt = snapshot.SyncedAt;
        }
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid accountId, CancellationToken ct = default)
    {
        var existing = await context.AccountSnapshots.FindAsync([accountId], ct);
        if (existing is not null)
        {
            context.AccountSnapshots.Remove(existing);
            await context.SaveChangesAsync(ct);
        }
    }

    public Task<bool> ExistsByCurrencyAsync(string currencyCode, CancellationToken ct = default)
        => context.AccountSnapshots.AnyAsync(a => a.Currency == currencyCode && a.IsActive, ct);
}
