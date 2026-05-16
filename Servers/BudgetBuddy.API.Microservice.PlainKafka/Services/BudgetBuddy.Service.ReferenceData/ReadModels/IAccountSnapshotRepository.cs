namespace BudgetBuddy.Service.ReferenceData.ReadModels;

public interface IAccountSnapshotRepository
{
    Task<AccountSnapshot?> FindAsync(Guid accountId, CancellationToken ct);
    Task<List<AccountSnapshot>> FindByUserAsync(string userId, CancellationToken ct);
    Task<bool> ExistsByCurrencyAsync(string currencyCode, CancellationToken ct);
    Task UpsertAsync(AccountSnapshot snapshot, CancellationToken ct);
    Task DeleteAsync(Guid accountId, CancellationToken ct);
}
