using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Service.ReferenceData.ReadModels;

public class AccountSnapshotRepository(ReferenceDataDbContext context) : IAccountSnapshotRepository
{
    public Task<AccountSnapshot?> FindAsync(Guid accountId, CancellationToken ct)
        => context.AccountSnapshots.FirstOrDefaultAsync(a => a.AccountId == accountId, ct);

    public Task<List<AccountSnapshot>> FindByUserAsync(string userId, CancellationToken ct)
        => context.AccountSnapshots.Where(a => a.UserId == userId).ToListAsync(ct);

    public Task<bool> ExistsByCurrencyAsync(string currencyCode, CancellationToken ct)
        => context.AccountSnapshots
            .AnyAsync(a => a.Currency == currencyCode.ToUpperInvariant(), ct);

    public async Task UpsertAsync(AccountSnapshot snapshot, CancellationToken ct)
    {
        var existing = await context.AccountSnapshots
            .FirstOrDefaultAsync(a => a.AccountId == snapshot.AccountId, ct);

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
            existing.Version  = snapshot.Version;
            existing.SyncedAt = snapshot.SyncedAt;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid accountId, CancellationToken ct)
    {
        var existing = await context.AccountSnapshots
            .FirstOrDefaultAsync(a => a.AccountId == accountId, ct);

        if (existing is not null)
        {
            context.AccountSnapshots.Remove(existing);
            await context.SaveChangesAsync(ct);
        }
    }
}
