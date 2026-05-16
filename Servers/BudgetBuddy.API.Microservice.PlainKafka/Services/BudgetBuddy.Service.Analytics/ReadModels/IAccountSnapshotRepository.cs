namespace BudgetBuddy.Service.Analytics.ReadModels;

public interface IAccountSnapshotRepository
{
    Task<AccountSnapshot?> FindAsync(Guid accountId, CancellationToken ct = default);
    Task<List<AccountSnapshot>> FindByUserAsync(string userId, CancellationToken ct = default);
    Task UpsertAsync(AccountSnapshot snapshot, CancellationToken ct = default);
    Task DeleteAsync(Guid accountId, CancellationToken ct = default);
    Task<bool> ExistsByCurrencyAsync(string currencyCode, CancellationToken ct = default);
}
