using BudgetBuddy.Shared.Messages.Contracts.Accounts;

namespace BudgetBuddy.Service.Transactions.ReadModels;

/// <summary>
/// Implements IAccountOwnershipService using the local AccountSnapshot read model
/// maintained via the accounts.changelog compacted Kafka topic.
/// No cross-service HTTP calls — data is always available locally.
/// </summary>
public class AccountOwnershipService(IAccountSnapshotRepository repository) : IAccountOwnershipService
{
    public async Task<bool> AccountBelongsToUserAsync(
        Guid accountId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.FindAsync(accountId, cancellationToken);
        return snapshot is { IsActive: true } && snapshot.UserId == userId;
    }

    public async Task<AccountBasicInfo?> GetAccountInfoAsync(
        Guid accountId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.FindAsync(accountId, cancellationToken);
        if (snapshot is null || !snapshot.IsActive || snapshot.UserId != userId)
        {
            return null;
        }

        return new AccountBasicInfo(snapshot.AccountId, snapshot.Name);
    }

    public async Task<Dictionary<string, Guid>> GetUserAccountNameMapAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await repository.FindByUserAsync(userId, cancellationToken);
        return snapshots
            .Where(s => s.IsActive)
            .ToDictionary(s => s.Name, s => s.AccountId, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<bool> IsCurrencyInUseAsync(
        string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await repository.FindByCurrencyAsync(currencyCode, cancellationToken);
        return snapshots.Exists(s => s.IsActive);
    }

    public async Task<Dictionary<Guid, string>> GetAccountNamesByIdsAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken = default)
    {
        var snapshots = await repository.FindByIdsAsync(accountIds, cancellationToken);
        return snapshots.ToDictionary(s => s.AccountId, s => s.Name);
    }
}
