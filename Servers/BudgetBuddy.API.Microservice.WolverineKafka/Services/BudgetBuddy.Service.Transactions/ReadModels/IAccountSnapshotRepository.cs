namespace BudgetBuddy.Service.Transactions.ReadModels;

public interface IAccountSnapshotRepository
{
    Task<AccountSnapshot?> FindAsync(Guid accountId, CancellationToken ct);
    Task<List<AccountSnapshot>> FindByIdsAsync(IEnumerable<Guid> accountIds, CancellationToken ct);
    Task<List<AccountSnapshot>> FindByUserAsync(string userId, CancellationToken ct);
    Task<List<AccountSnapshot>> FindByCurrencyAsync(string currencyCode, CancellationToken ct);
    Task UpsertAsync(AccountSnapshot snapshot, CancellationToken ct);
    Task DeleteAsync(Guid accountId, CancellationToken ct);
}
