using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Shared.Messages.Contracts.Accounts;
using BudgetBuddy.Shared.Messages.Contracts.Financial;

namespace BudgetBuddy.Service.Analytics.ReadModels;

public class AccountBalanceService(
    AnalyticsDbContext context,
    ICurrencyConversionService currencyConversion) : IAccountBalanceService
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

        var accountIds = accounts.Select(a => a.AccountId).ToList();
        var txQuery = context.Transactions.Where(t => accountIds.Contains(t.AccountId));
        if (upToDate.HasValue)
        {
            txQuery = txQuery.Where(t => t.TransactionDate <= upToDate.Value);
        }

        var totals = await txQuery
            .GroupBy(t => new { t.AccountId, t.TransactionType })
            .Select(g => new { g.Key.AccountId, g.Key.TransactionType, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        var totalsMap = totals.ToDictionary(r => (r.AccountId, r.TransactionType), r => r.Total);

        var result = new List<AccountBalanceResult>();
        foreach (var account in accounts)
        {
            var income  = totalsMap.GetValueOrDefault((account.AccountId, TransactionType.Income));
            var expense = totalsMap.GetValueOrDefault((account.AccountId, TransactionType.Expense));
            var balance = account.Balance + income - expense;
            var converted = account.Currency == targetCurrency
                ? balance
                : await currencyConversion.ConvertAsync(balance, account.Currency, targetCurrency, cancellationToken);
            result.Add(new AccountBalanceResult(account.AccountId, account.Name, account.Currency, balance, converted));
        }
        return result;
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

    public Task<int> GetAccountCountAsync(string userId, CancellationToken cancellationToken = default)
        => context.AccountSnapshots.CountAsync(a => a.UserId == userId && a.IsActive, cancellationToken);

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
