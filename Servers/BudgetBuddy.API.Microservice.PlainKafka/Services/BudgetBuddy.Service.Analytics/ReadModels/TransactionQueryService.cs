using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Shared.Messages.Contracts.Transactions;

namespace BudgetBuddy.Service.Analytics.ReadModels;

public class TransactionQueryService(AnalyticsDbContext context) : ITransactionQueryService
{
    public Task<List<TransactionMonthlySummaryRow>> GetMonthlyTotalsAsync(
        string userId, LocalDate startDate, LocalDate endDate, CancellationToken cancellationToken = default)
        => context.Transactions
            .Where(t => t.UserId == userId
                && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .GroupBy(t => new { t.CurrencyCode, t.TransactionType })
            .Select(g => new TransactionMonthlySummaryRow(
                g.Key.CurrencyCode, g.Key.TransactionType, g.Sum(t => t.Amount), g.Count()))
            .ToListAsync(cancellationToken);

    public Task<List<CategorySpendingRow>> GetExpensesByCategoryIdsAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
    {
        var ids = categoryIds.ToList();
        return context.Transactions
            .Where(t => t.UserId == userId
                && t.TransactionDate >= startDate && t.TransactionDate <= endDate
                && t.TransactionType == TransactionType.Expense
                && t.CategoryId.HasValue && ids.Contains(t.CategoryId.Value))
            .GroupBy(t => new { t.CategoryId, t.CurrencyCode })
            .Select(g => new CategorySpendingRow(g.Key.CategoryId!.Value, g.Key.CurrencyCode, g.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);
    }

    public Task<List<TransactionRecentItem>> GetRecentTransactionsAsync(
        string userId, int count, CancellationToken cancellationToken = default)
        => context.Transactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.TransactionDate)
            .Take(count)
            .Select(t => new TransactionRecentItem(
                t.TransactionId, t.TransactionDate, t.AccountId,
                t.CategoryId, t.TransactionType, t.Amount, t.CurrencyCode))
            .ToListAsync(cancellationToken);

    public Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryAsync(
        string userId, LocalDate startDate, LocalDate endDate, CancellationToken cancellationToken = default)
        => context.Transactions
            .Where(t => t.UserId == userId
                && t.TransactionDate >= startDate && t.TransactionDate <= endDate
                && t.TransactionType == TransactionType.Expense)
            .GroupBy(t => new { t.CategoryId, t.CurrencyCode })
            .Select(g => new CategoryExpenseRow(g.Key.CategoryId, g.Key.CurrencyCode, g.Sum(t => t.Amount), g.Count()))
            .ToListAsync(cancellationToken);

    public Task<List<TransactionCurrencyTypeTotals>> GetTotalsByCurrencyAndTypeAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId, CancellationToken cancellationToken = default)
        => context.Transactions
            .Where(t => t.UserId == userId
                && (startDate == null || t.TransactionDate >= startDate.Value)
                && (endDate == null || t.TransactionDate <= endDate.Value)
                && (accountId == null || t.AccountId == accountId.Value))
            .GroupBy(t => new { t.CurrencyCode, t.TransactionType })
            .Select(g => new TransactionCurrencyTypeTotals(
                g.Key.CurrencyCode, g.Key.TransactionType, g.Sum(t => t.Amount), g.Count()))
            .ToListAsync(cancellationToken);

    public Task<List<TransactionMonthlyGroupRow>> GetGroupedByMonthCurrencyTypeAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId, CancellationToken cancellationToken = default)
        => context.Transactions
            .Where(t => t.UserId == userId
                && (startDate == null || t.TransactionDate >= startDate.Value)
                && (endDate == null || t.TransactionDate <= endDate.Value)
                && (accountId == null || t.AccountId == accountId.Value))
            .GroupBy(t => new
            {
                t.TransactionDate.Year,
                t.TransactionDate.Month,
                t.CurrencyCode,
                t.TransactionType
            })
            .Select(g => new TransactionMonthlyGroupRow(
                g.Key.Year, g.Key.Month, g.Key.CurrencyCode, g.Key.TransactionType, g.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);

    public Task<List<TransactionMonthlySummaryRow>> GetMonthlySummaryGroupedAsync(
        string userId, LocalDate startDate, LocalDate endDate, Guid? accountId, CancellationToken cancellationToken = default)
        => context.Transactions
            .Where(t => t.UserId == userId
                && t.TransactionDate >= startDate && t.TransactionDate <= endDate
                && (accountId == null || t.AccountId == accountId.Value))
            .GroupBy(t => new { t.CurrencyCode, t.TransactionType })
            .Select(g => new TransactionMonthlySummaryRow(
                g.Key.CurrencyCode, g.Key.TransactionType, g.Sum(t => t.Amount), g.Count()))
            .ToListAsync(cancellationToken);

    public Task<List<TransactionDailySummaryRow>> GetDailyBreakdownAsync(
        string userId, LocalDate startDate, LocalDate endDate, Guid? accountId, CancellationToken cancellationToken = default)
        => context.Transactions
            .Where(t => t.UserId == userId
                && t.TransactionDate >= startDate && t.TransactionDate <= endDate
                && (accountId == null || t.AccountId == accountId.Value))
            .GroupBy(t => new { t.TransactionDate.Day, t.TransactionType, t.CurrencyCode })
            .Select(g => new TransactionDailySummaryRow(
                g.Key.Day, g.Key.TransactionType, g.Key.CurrencyCode, g.Sum(t => t.Amount), g.Count()))
            .ToListAsync(cancellationToken);

    public Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsBeforeDateAsync(
        IEnumerable<Guid> accountIds, LocalDate beforeDate, CancellationToken cancellationToken = default)
    {
        var ids = accountIds.ToList();
        return context.Transactions
            .Where(t => ids.Contains(t.AccountId) && t.TransactionDate < beforeDate)
            .GroupBy(t => new { t.AccountId, t.TransactionType })
            .Select(g => new AccountBalanceTransactionRow(g.Key.AccountId, g.Key.TransactionType, g.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);
    }

    public Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsInRangeAsync(
        IEnumerable<Guid> accountIds, LocalDate startDate, LocalDate endDate, CancellationToken cancellationToken = default)
    {
        var ids = accountIds.ToList();
        return context.Transactions
            .Where(t => ids.Contains(t.AccountId)
                && t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .GroupBy(t => new { t.AccountId, t.TransactionType })
            .Select(g => new AccountBalanceTransactionRow(g.Key.AccountId, g.Key.TransactionType, g.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);
    }

    public Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryForReportAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId, CancellationToken cancellationToken = default)
        => context.Transactions
            .Where(t => t.UserId == userId
                && t.TransactionType == TransactionType.Expense
                && (startDate == null || t.TransactionDate >= startDate.Value)
                && (endDate == null || t.TransactionDate <= endDate.Value)
                && (accountId == null || t.AccountId == accountId.Value))
            .GroupBy(t => new { t.CategoryId, t.CurrencyCode })
            .Select(g => new CategoryExpenseRow(g.Key.CategoryId, g.Key.CurrencyCode, g.Sum(t => t.Amount), g.Count()))
            .ToListAsync(cancellationToken);

    public Task<List<CategorySpendingRow>> GetCategorySpendingAsync(
        string userId, LocalDate startDate, LocalDate endDate, CancellationToken cancellationToken = default)
        => context.Transactions
            .Where(t => t.UserId == userId
                && t.TransactionDate >= startDate && t.TransactionDate <= endDate
                && t.TransactionType == TransactionType.Expense
                && t.CategoryId.HasValue)
            .GroupBy(t => new { t.CategoryId, t.CurrencyCode })
            .Select(g => new CategorySpendingRow(g.Key.CategoryId!.Value, g.Key.CurrencyCode, g.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);

    public async Task<Dictionary<Guid, decimal>> GetExpensesByCategoryAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
    {
        var ids = categoryIds.ToList();
        var rows = await context.Transactions
            .Where(t => t.UserId == userId
                && t.TransactionDate >= startDate && t.TransactionDate <= endDate
                && t.TransactionType == TransactionType.Expense
                && t.CategoryId.HasValue && ids.Contains(t.CategoryId.Value))
            .GroupBy(t => t.CategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(r => r.CategoryId, r => r.Total);
    }

    public async Task<LocalDate?> GetEarliestTransactionDateAsync(CancellationToken cancellationToken = default)
    {
        if (!await context.Transactions.AnyAsync(cancellationToken))
        {
            return null;
        }

        return await context.Transactions.MinAsync(t => t.TransactionDate, cancellationToken);
    }
}
