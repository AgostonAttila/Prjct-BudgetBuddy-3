using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Shared.Messages.Contracts.Accounts;

namespace BudgetBuddy.Service.Analytics.ReadModels;

public class AccountTransactionSummaryService(AnalyticsDbContext context) : IAccountTransactionSummary
{
    public async Task<List<AccountTransactionAggregate>> GetAggregatesForAccountsAsync(
        IEnumerable<Guid> accountIds,
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default)
    {
        var ids = accountIds.ToList();
        var query = context.Transactions.Where(t => ids.Contains(t.AccountId));
        if (upToDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= upToDate.Value);
        }

        var rows = await query
            .GroupBy(t => new { t.AccountId, t.TransactionType })
            .Select(g => new { g.Key.AccountId, g.Key.TransactionType, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.AccountId)
            .Select(g => new AccountTransactionAggregate(
                g.Key,
                g.Where(r => r.TransactionType == TransactionType.Income).Sum(r => r.Total),
                g.Where(r => r.TransactionType == TransactionType.Expense).Sum(r => r.Total)))
            .ToList();
    }

    public async Task<SingleAccountTransactionAggregate> GetAggregateForAccountAsync(
        Guid accountId, CancellationToken cancellationToken = default)
    {
        var rows = await context.Transactions
            .Where(t => t.AccountId == accountId)
            .GroupBy(t => t.TransactionType)
            .Select(g => new { TransactionType = g.Key, Total = g.Sum(t => t.Amount), Count = g.Count() })
            .ToListAsync(cancellationToken);

        var income  = rows.Find(r => r.TransactionType == TransactionType.Income);
        var expense = rows.Find(r => r.TransactionType == TransactionType.Expense);
        return new SingleAccountTransactionAggregate(
            income?.Total ?? 0m,
            expense?.Total ?? 0m,
            rows.Sum(r => r.Count));
    }
}
