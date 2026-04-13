using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Infrastructure.Persistence.Repositories;

public class AccountRepository(AppDbContext context) : UserOwnedRepository<Account>(context), IAccountRepository
{
    public async Task<List<AccountSummary>> GetSummariesAsync(string userId, CancellationToken ct = default)
    {
        return await Context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new AccountSummary(
                a.Id,
                a.Name,
                a.Description,
                a.DefaultCurrencyCode,
                a.InitialBalance,
                a.Transactions.Count()))
            .ToListAsync(ct);
    }

    public async Task<AccountBalance?> GetBalanceAsync(Guid accountId, string userId, CancellationToken ct = default)
    {
        var account = await Context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == accountId && a.UserId == userId)
            .Select(a => new { a.Id, a.Name, a.InitialBalance })
            .FirstOrDefaultAsync(ct);

        if (account == null)
            return null;

        var transactionsByType = await Context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .GroupBy(t => t.TransactionType)
            .Select(g => new { TransactionType = g.Key, Total = g.Sum(t => t.Amount), Count = g.Count() })
            .ToListAsync(ct);

        var totalIncome = transactionsByType
            .FirstOrDefault(x => x.TransactionType == TransactionType.Income)?.Total ?? 0;
        var totalExpense = transactionsByType
            .FirstOrDefault(x => x.TransactionType == TransactionType.Expense)?.Total ?? 0;
        var transactionCount = transactionsByType.Sum(x => x.Count);

        return new AccountBalance(account.Id, account.Name, account.InitialBalance, totalIncome, totalExpense, transactionCount);
    }

    public async Task<List<AccountBalanceData>> GetBalanceDataAsync(string userId, CancellationToken ct = default)
    {
        return await Context.Accounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new AccountBalanceData(
                a.Id,
                a.Name,
                a.InitialBalance,
                a.DefaultCurrencyCode ?? "USD"))
            .ToListAsync(ct);
    }

    public async Task<bool> HasDefaultCurrencyAsync(string currencyCode, CancellationToken ct = default)
    {
        return await Context.Accounts
            .AnyAsync(a => a.DefaultCurrencyCode == currencyCode, ct);
    }
}
