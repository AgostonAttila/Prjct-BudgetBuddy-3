namespace BudgetBuddy.Service.Transactions.ReadModels;

public interface IAccountSnapshotService
{
    /// <summary>
    /// Returns the account snapshot or throws NotFoundException / DomainException.
    /// Checks L1 (in-memory) → L2 (Redis) → L3 (local DB).
    /// </summary>
    Task<AccountSnapshot> GetOrThrowAsync(Guid accountId, string userId, CancellationToken ct);

    Task PopulateAsync(AccountSnapshot snapshot, CancellationToken ct);
    Task InvalidateAsync(Guid accountId, CancellationToken ct);
}
