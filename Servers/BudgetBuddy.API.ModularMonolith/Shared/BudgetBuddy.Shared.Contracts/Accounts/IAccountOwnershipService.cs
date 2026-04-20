namespace BudgetBuddy.Shared.Contracts.Accounts;

public record AccountBasicInfo(Guid Id, string Name);

public interface IAccountOwnershipService
{
    Task<bool> AccountBelongsToUserAsync(
        Guid accountId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<AccountBasicInfo?> GetAccountInfoAsync(
        Guid accountId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, Guid>> GetUserAccountNameMapAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsCurrencyInUseAsync(
        string currencyCode,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, string>> GetAccountNamesByIdsAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken = default);
}
