using BudgetBuddy.Shared.Messages.Contracts.Accounts;

namespace BudgetBuddy.Service.Analytics.ReadModels;

public class AccountOwnershipService(IAccountSnapshotRepository repository) : IAccountOwnershipService
{
    public async Task<bool> AccountBelongsToUserAsync(
        Guid accountId, string userId, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.FindAsync(accountId, cancellationToken);
        return snapshot is not null && snapshot.UserId == userId && snapshot.IsActive;
    }

    public async Task<AccountBasicInfo?> GetAccountInfoAsync(
        Guid accountId, string userId, CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.FindAsync(accountId, cancellationToken);
        if (snapshot is null || snapshot.UserId != userId)
        {
            return null;
        }

        return new AccountBasicInfo(snapshot.AccountId, snapshot.Name);
    }

    public async Task<Dictionary<string, Guid>> GetUserAccountNameMapAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var snapshots = await repository.FindByUserAsync(userId, cancellationToken);
        return snapshots.ToDictionary(s => s.Name, s => s.AccountId);
    }

    public Task<bool> IsCurrencyInUseAsync(
        string currencyCode, CancellationToken cancellationToken = default)
        => repository.ExistsByCurrencyAsync(currencyCode, cancellationToken);

    public async Task<Dictionary<Guid, string>> GetAccountNamesByIdsAsync(
        IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, string>();
        foreach (var id in accountIds)
        {
            var snapshot = await repository.FindAsync(id, cancellationToken);
            if (snapshot is not null)
            {
                result[id] = snapshot.Name;
            }
        }
        return result;
    }
}
