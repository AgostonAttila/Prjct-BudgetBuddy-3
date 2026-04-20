using BudgetBuddy.Shared.Contracts.Transactions;

namespace BudgetBuddy.Module.Transactions.Features.Transactions.Services;

public class TransactionQueryService(TransactionsDbContext context) : ITransactionQueryService
{
    public Task<List<TransactionMonthlySummaryRow>> GetMonthlyTotalsAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default) =>
        context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId &&
                        t.TransactionDate >= startDate &&
                        t.TransactionDate <= endDate &&
                        t.CurrencyCode != null)
            .GroupBy(t => new { t.TransactionType, t.CurrencyCode })
            .Select(g => new TransactionMonthlySummaryRow(
                g.Key.CurrencyCode!.ToUpper(),
                g.Key.TransactionType,
                g.Sum(t => t.Amount),
                g.Count()))
            .ToListAsync(cancellationToken);

    public Task<List<CategorySpendingRow>> GetExpensesByCategoryIdsAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default)
    {
        var ids = categoryIds.ToList();
        return context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId &&
                        t.TransactionDate >= startDate &&
                        t.TransactionDate <= endDate &&
                        t.TransactionType == TransactionType.Expense &&
                        t.CategoryId.HasValue &&
                        t.CurrencyCode != null &&
                        ids.Contains(t.CategoryId.Value))
            .GroupBy(t => new { t.CategoryId, t.CurrencyCode })
            .Select(g => new CategorySpendingRow(
                g.Key.CategoryId!.Value,
                g.Key.CurrencyCode!.ToUpper(),
                g.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);
    }

    public Task<List<TransactionRecentItem>> GetRecentTransactionsAsync(
        string userId, int count,
        CancellationToken cancellationToken = default) =>
        context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Take(count)
            .Select(t => new TransactionRecentItem(
                t.Id,
                t.TransactionDate,
                t.AccountId,
                t.CategoryId,
                t.TransactionType,
                t.Amount,
                t.CurrencyCode))
            .ToListAsync(cancellationToken);

    public Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default) =>
        context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId &&
                        t.TransactionDate >= startDate &&
                        t.TransactionDate <= endDate &&
                        t.TransactionType == TransactionType.Expense &&
                        t.CategoryId.HasValue &&
                        t.CurrencyCode != null)
            .GroupBy(t => new { t.CategoryId, t.CurrencyCode })
            .Select(g => new CategoryExpenseRow(
                g.Key.CategoryId,
                g.Key.CurrencyCode!.ToUpper(),
                g.Sum(t => t.Amount),
                g.Count()))
            .ToListAsync(cancellationToken);

    public Task<List<TransactionCurrencyTypeTotals>> GetTotalsByCurrencyAndTypeAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(userId, startDate, endDate, accountId);
        return query
            .AsNoTracking()
            .Where(t => t.CurrencyCode != null)
            .GroupBy(t => new { t.CurrencyCode, t.TransactionType })
            .Select(g => new TransactionCurrencyTypeTotals(
                g.Key.CurrencyCode!.ToUpper(),
                g.Key.TransactionType,
                g.Sum(t => t.Amount),
                g.Count()))
            .ToListAsync(cancellationToken);
    }

    public Task<List<TransactionMonthlyGroupRow>> GetGroupedByMonthCurrencyTypeAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(userId, startDate, endDate, accountId);
        return query
            .AsNoTracking()
            .Where(t => t.CurrencyCode != null)
            .GroupBy(t => new
            {
                t.TransactionDate.Year,
                t.TransactionDate.Month,
                t.CurrencyCode,
                t.TransactionType
            })
            .Select(g => new TransactionMonthlyGroupRow(
                g.Key.Year,
                g.Key.Month,
                g.Key.CurrencyCode!.ToUpper(),
                g.Key.TransactionType,
                g.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);
    }

    public Task<List<TransactionMonthlySummaryRow>> GetMonthlySummaryGroupedAsync(
        string userId, LocalDate startDate, LocalDate endDate, Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Transactions
            .Where(t => t.UserId == userId &&
                        t.TransactionDate >= startDate &&
                        t.TransactionDate <= endDate);
        if (accountId.HasValue)
            query = query.Where(t => t.AccountId == accountId.Value);

        return query
            .AsNoTracking()
            .Where(t => t.CurrencyCode != null)
            .GroupBy(t => new { t.TransactionType, t.CurrencyCode })
            .Select(g => new TransactionMonthlySummaryRow(
                g.Key.CurrencyCode!.ToUpper(),
                g.Key.TransactionType,
                g.Sum(t => t.Amount),
                g.Count()))
            .ToListAsync(cancellationToken);
    }

    public Task<List<TransactionDailySummaryRow>> GetDailyBreakdownAsync(
        string userId, LocalDate startDate, LocalDate endDate, Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Transactions
            .Where(t => t.UserId == userId &&
                        t.TransactionDate >= startDate &&
                        t.TransactionDate <= endDate);
        if (accountId.HasValue)
            query = query.Where(t => t.AccountId == accountId.Value);

        return query
            .AsNoTracking()
            .Where(t => t.CurrencyCode != null)
            .GroupBy(t => new { t.TransactionDate.Day, t.TransactionType, t.CurrencyCode })
            .Select(g => new TransactionDailySummaryRow(
                g.Key.Day,
                g.Key.TransactionType,
                g.Key.CurrencyCode!.ToUpper(),
                g.Sum(t => t.Amount),
                g.Count()))
            .ToListAsync(cancellationToken);
    }

    public Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsBeforeDateAsync(
        IEnumerable<Guid> accountIds, LocalDate beforeDate,
        CancellationToken cancellationToken = default)
    {
        var ids = accountIds.ToList();
        return context.Transactions
            .AsNoTracking()
            .Where(t => ids.Contains(t.AccountId) && t.TransactionDate < beforeDate)
            .GroupBy(t => new { t.AccountId, t.TransactionType })
            .Select(g => new AccountBalanceTransactionRow(
                g.Key.AccountId,
                g.Key.TransactionType,
                g.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);
    }

    public Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsInRangeAsync(
        IEnumerable<Guid> accountIds, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default)
    {
        var ids = accountIds.ToList();
        return context.Transactions
            .AsNoTracking()
            .Where(t => ids.Contains(t.AccountId) &&
                        t.TransactionDate >= startDate &&
                        t.TransactionDate <= endDate)
            .GroupBy(t => new { t.AccountId, t.TransactionType })
            .Select(g => new AccountBalanceTransactionRow(
                g.Key.AccountId,
                g.Key.TransactionType,
                g.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);
    }

    public Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryForReportAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default)
    {
        var query = context.Transactions
            .Where(t => t.UserId == userId && t.TransactionType == TransactionType.Expense);
        if (startDate.HasValue) query = query.Where(t => t.TransactionDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(t => t.TransactionDate <= endDate.Value);
        if (accountId.HasValue) query = query.Where(t => t.AccountId == accountId.Value);

        return query
            .AsNoTracking()
            .Where(t => t.CurrencyCode != null)
            .GroupBy(t => new { t.CategoryId, t.CurrencyCode })
            .Select(g => new CategoryExpenseRow(
                g.Key.CategoryId,
                g.Key.CurrencyCode!.ToUpper(),
                g.Sum(t => t.Amount),
                g.Count()))
            .ToListAsync(cancellationToken);
    }

    public Task<List<CategorySpendingRow>> GetCategorySpendingAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default) =>
        context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId &&
                        t.TransactionDate >= startDate &&
                        t.TransactionDate <= endDate &&
                        t.TransactionType == TransactionType.Expense &&
                        t.CategoryId.HasValue &&
                        t.CurrencyCode != null)
            .GroupBy(t => new { t.CategoryId, t.CurrencyCode })
            .Select(g => new CategorySpendingRow(
                g.Key.CategoryId!.Value,
                g.Key.CurrencyCode!.ToUpper(),
                g.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);

    public Task<Dictionary<Guid, decimal>> GetExpensesByCategoryAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default)
    {
        var ids = categoryIds.ToList();
        return context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId &&
                        t.TransactionType == TransactionType.Expense &&
                        t.TransactionDate >= startDate &&
                        t.TransactionDate <= endDate &&
                        t.CategoryId.HasValue &&
                        ids.Contains(t.CategoryId!.Value))
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key!.Value, Amount = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Amount, cancellationToken);
    }

    public Task<LocalDate?> GetEarliestTransactionDateAsync(
        CancellationToken cancellationToken = default) =>
        context.Transactions.MinAsync(t => (LocalDate?)t.TransactionDate, cancellationToken);

    private IQueryable<Transaction> ApplyFilters(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId)
    {
        var query = context.Transactions.Where(t => t.UserId == userId);
        if (startDate.HasValue) query = query.Where(t => t.TransactionDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(t => t.TransactionDate <= endDate.Value);
        if (accountId.HasValue) query = query.Where(t => t.AccountId == accountId.Value);
        return query;
    }
}
