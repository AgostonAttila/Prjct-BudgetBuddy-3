using BudgetBuddy.Service.Budgets.ReadModels;

namespace BudgetBuddy.Service.Budgets.ReadModels;

/// <summary>
/// Budgets-local implementation of ITransactionQueryService.
/// Backed by CategorySpendingAggregate read model (built from Kafka transaction events).
/// Only implements the two methods actually used by the Budgets service.
/// </summary>
public class TransactionQueryService(ICategorySpendingRepository repository) : ITransactionQueryService
{
    public async Task<List<CategorySpendingRow>> GetCategorySpendingAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default)
    {
        var rows = await repository.GetByUserAndDateRangeAsync(
            userId,
            startDate.Year, startDate.Month,
            endDate.Year,   endDate.Month,
            cancellationToken);

        return rows
            .GroupBy(r => new { r.CategoryId, r.CurrencyCode })
            .Select(g => new CategorySpendingRow(
                g.Key.CategoryId,
                g.Key.CurrencyCode,
                g.Sum(r => r.TotalExpense)))
            .ToList();
    }

    public async Task<Dictionary<Guid, decimal>> GetExpensesByCategoryAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default)
    {
        var rows = await repository.GetByUserCategoryAndDateRangeAsync(
            userId, categoryIds,
            startDate.Year, startDate.Month,
            endDate.Year,   endDate.Month,
            cancellationToken);

        return rows
            .GroupBy(r => r.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalExpense));
    }

    // ── Unimplemented methods — not used by Budgets service ──────────────────

    public Task<List<TransactionMonthlySummaryRow>> GetMonthlyTotalsAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");

    public Task<List<CategorySpendingRow>> GetExpensesByCategoryIdsAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");

    public Task<List<TransactionRecentItem>> GetRecentTransactionsAsync(
        string userId, int count,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");

    public Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");

    public Task<List<TransactionCurrencyTypeTotals>> GetTotalsByCurrencyAndTypeAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");

    public Task<List<TransactionMonthlyGroupRow>> GetGroupedByMonthCurrencyTypeAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");

    public Task<List<TransactionMonthlySummaryRow>> GetMonthlySummaryGroupedAsync(
        string userId, LocalDate startDate, LocalDate endDate, Guid? accountId,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");

    public Task<List<TransactionDailySummaryRow>> GetDailyBreakdownAsync(
        string userId, LocalDate startDate, LocalDate endDate, Guid? accountId,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");

    public Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsBeforeDateAsync(
        IEnumerable<Guid> accountIds, LocalDate beforeDate,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");

    public Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsInRangeAsync(
        IEnumerable<Guid> accountIds, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");

    public Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryForReportAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");

    public Task<LocalDate?> GetEarliestTransactionDateAsync(
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Not implemented in Budgets service.");
}
