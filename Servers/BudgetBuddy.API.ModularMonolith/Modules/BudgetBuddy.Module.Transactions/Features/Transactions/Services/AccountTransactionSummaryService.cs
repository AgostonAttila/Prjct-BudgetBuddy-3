using BudgetBuddy.Shared.Contracts.Accounts;
using NodaTime;

namespace BudgetBuddy.Module.Transactions.Features.Transactions.Services;

/// <summary>
/// Implements IAccountTransactionSummary so that the Accounts module can query
/// transaction aggregates without a direct project reference to Module.Transactions.
/// </summary>
public class AccountTransactionSummaryService(TransactionsDbContext context) : IAccountTransactionSummary
{
    public async Task<List<AccountTransactionAggregate>> GetAggregatesForAccountsAsync(
        IEnumerable<Guid> accountIds,
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default)
    {
        var ids = accountIds.ToList();

        var query = context.Transactions
            .AsNoTracking()
            .Where(t => ids.Contains(t.AccountId));

        if (upToDate.HasValue)
            query = query.Where(t => t.TransactionDate <= upToDate.Value);

        var rawAggregates = await query
            .GroupBy(t => new { t.AccountId, t.TransactionType })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.TransactionType,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(cancellationToken);

        return rawAggregates
            .GroupBy(a => a.AccountId)
            .Select(g => new AccountTransactionAggregate(
                AccountId: g.Key,
                TotalIncome: g.Where(x => x.TransactionType == TransactionType.Income).Sum(x => x.Total),
                TotalExpense: g.Where(x => x.TransactionType == TransactionType.Expense).Sum(x => x.Total)
            ))
            .ToList();
    }

    public async Task<SingleAccountTransactionAggregate> GetAggregateForAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var rawAggregates = await context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .GroupBy(t => t.TransactionType)
            .Select(g => new
            {
                TransactionType = g.Key,
                Total = g.Sum(t => t.Amount),
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var totalIncome = rawAggregates
            .FirstOrDefault(x => x.TransactionType == TransactionType.Income)?.Total ?? 0;
        var totalExpense = rawAggregates
            .FirstOrDefault(x => x.TransactionType == TransactionType.Expense)?.Total ?? 0;
        var transactionCount = rawAggregates.Sum(x => x.Count);

        return new SingleAccountTransactionAggregate(totalIncome, totalExpense, transactionCount);
    }
}
