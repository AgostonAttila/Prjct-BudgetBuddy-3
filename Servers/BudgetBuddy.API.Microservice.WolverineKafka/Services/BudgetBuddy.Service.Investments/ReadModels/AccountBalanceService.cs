using BudgetBuddy.Service.Investments.Persistence;
using BudgetBuddy.Shared.Messages.Contracts.Accounts;
using BudgetBuddy.Shared.Messages.Contracts.Financial;

namespace BudgetBuddy.Service.Investments.ReadModels;

public class AccountBalanceService(
    InvestmentsDbContext context,
    ICurrencyConversionService currencyConversionService) : IAccountBalanceService
{
    public async Task<List<AccountBalanceResult>> CalculateAccountBalancesAsync(
        string userId,
        string targetCurrency = "USD",
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default)
    {
        var accounts = await context.AccountSnapshots
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync(cancellationToken);

        var results = new List<AccountBalanceResult>(accounts.Count);
        foreach (var a in accounts)
        {
            var convertedBalance = string.Equals(a.Currency, targetCurrency, StringComparison.OrdinalIgnoreCase)
                ? a.Balance
                : await currencyConversionService.ConvertAsync(a.Balance, a.Currency, targetCurrency, cancellationToken);

            results.Add(new AccountBalanceResult(a.AccountId, a.Name, a.Currency, a.Balance, convertedBalance));
        }

        return results;
    }

    public async Task<decimal> CalculateTotalBalanceAsync(
        string userId,
        string targetCurrency = "USD",
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default)
    {
        var balances = await CalculateAccountBalancesAsync(userId, targetCurrency, upToDate, cancellationToken);
        return balances.Sum(b => b.ConvertedBalance);
    }

    public async Task<int> GetAccountCountAsync(string userId, CancellationToken cancellationToken = default)
        => await context.AccountSnapshots.CountAsync(a => a.UserId == userId && a.IsActive, cancellationToken);

    public async Task<List<AccountInitialBalanceResult>> GetAccountInitialBalancesAsync(
        string userId,
        Guid? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.AccountSnapshots.Where(a => a.UserId == userId && a.IsActive);
        if (accountId.HasValue)
        {
            query = query.Where(a => a.AccountId == accountId.Value);
        }

        return await query
            .Select(a => new AccountInitialBalanceResult(a.AccountId, a.Balance, a.Currency))
            .ToListAsync(cancellationToken);
    }
}
