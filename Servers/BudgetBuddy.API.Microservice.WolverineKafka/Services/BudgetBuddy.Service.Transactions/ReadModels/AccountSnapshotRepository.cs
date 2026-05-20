using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Service.Transactions.ReadModels;

public class AccountSnapshotRepository : IAccountSnapshotRepository
{
    private readonly TransactionsDbContext _context;

    public AccountSnapshotRepository(TransactionsDbContext context) => _context = context;

    public Task<AccountSnapshot?> FindAsync(Guid accountId, CancellationToken ct)
        => _context.AccountSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == accountId, ct);

    public Task<List<AccountSnapshot>> FindByIdsAsync(IEnumerable<Guid> accountIds, CancellationToken ct)
        => _context.AccountSnapshots
            .Where(a => accountIds.Contains(a.AccountId))
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<List<AccountSnapshot>> FindByUserAsync(string userId, CancellationToken ct)
        => _context.AccountSnapshots
            .Where(a => a.UserId == userId)
            .AsNoTracking()
            .ToListAsync(ct);

    public Task<List<AccountSnapshot>> FindByCurrencyAsync(string currencyCode, CancellationToken ct)
        => _context.AccountSnapshots
            .Where(a => a.Currency == currencyCode.ToUpperInvariant())
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task UpsertAsync(AccountSnapshot snapshot, CancellationToken ct)
    {
        var existing = await _context.AccountSnapshots
            .FirstOrDefaultAsync(a => a.AccountId == snapshot.AccountId, ct);

        if (existing is null)
        {
            _context.AccountSnapshots.Add(snapshot);
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

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid accountId, CancellationToken ct)
    {
        var existing = await _context.AccountSnapshots
            .FirstOrDefaultAsync(a => a.AccountId == accountId, ct);

        if (existing is not null)
        {
            _context.AccountSnapshots.Remove(existing);
            await _context.SaveChangesAsync(ct);
        }
    }
}
