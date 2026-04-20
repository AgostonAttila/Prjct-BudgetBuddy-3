using BudgetBuddy.Shared.Contracts.Accounts;

namespace BudgetBuddy.Module.Accounts.Services;

public class AccountOwnershipService(AccountsDbContext context) : IAccountOwnershipService
{
    public Task<bool> AccountBelongsToUserAsync(
        Guid accountId,
        string userId,
        CancellationToken cancellationToken = default) =>
        context.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

    public async Task<AccountBasicInfo?> GetAccountInfoAsync(
        Guid accountId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == accountId && a.UserId == userId)
            .Select(a => new AccountBasicInfo(a.Id, a.Name))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<string, Guid>> GetUserAccountNameMapAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .ToDictionaryAsync(a => a.Name, a => a.Id, cancellationToken);
    }

    public Task<bool> IsCurrencyInUseAsync(
        string currencyCode,
        CancellationToken cancellationToken = default) =>
        context.Accounts
            .AnyAsync(a => a.DefaultCurrencyCode == currencyCode, cancellationToken);

    public async Task<Dictionary<Guid, string>> GetAccountNamesByIdsAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken = default)
    {
        var ids = accountIds.ToList();
        return await context.Accounts
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);
    }
}
