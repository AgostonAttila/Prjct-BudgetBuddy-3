using BudgetBuddy.Shared.Kernel.Enums;
using BudgetBuddy.Shared.Messages.Contracts.Transactions;
using NodaTime;

namespace BudgetBuddy.Service.Investments.ReadModels;

/// <summary>
/// Investments-local stub: the Investments service does not replicate transaction data.
/// Only GetEarliestTransactionDateAsync is used (by MarketDataBackfillService for FX backfill range).
/// Returns null so the backfill falls back to the earliest investment purchase date.
/// </summary>
public class TransactionQueryServiceStub : ITransactionQueryService
{
    public Task<LocalDate?> GetEarliestTransactionDateAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<LocalDate?>(null);

    public Task<List<TransactionMonthlySummaryRow>> GetMonthlyTotalsAsync(string userId, LocalDate startDate, LocalDate endDate, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<TransactionMonthlySummaryRow>());

    public Task<List<CategorySpendingRow>> GetExpensesByCategoryIdsAsync(string userId, LocalDate startDate, LocalDate endDate, IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<CategorySpendingRow>());

    public Task<List<TransactionRecentItem>> GetRecentTransactionsAsync(string userId, int count, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<TransactionRecentItem>());

    public Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryAsync(string userId, LocalDate startDate, LocalDate endDate, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<CategoryExpenseRow>());

    public Task<List<TransactionCurrencyTypeTotals>> GetTotalsByCurrencyAndTypeAsync(string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<TransactionCurrencyTypeTotals>());

    public Task<List<TransactionMonthlyGroupRow>> GetGroupedByMonthCurrencyTypeAsync(string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<TransactionMonthlyGroupRow>());

    public Task<List<TransactionMonthlySummaryRow>> GetMonthlySummaryGroupedAsync(string userId, LocalDate startDate, LocalDate endDate, Guid? accountId, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<TransactionMonthlySummaryRow>());

    public Task<List<TransactionDailySummaryRow>> GetDailyBreakdownAsync(string userId, LocalDate startDate, LocalDate endDate, Guid? accountId, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<TransactionDailySummaryRow>());

    public Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsBeforeDateAsync(IEnumerable<Guid> accountIds, LocalDate beforeDate, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<AccountBalanceTransactionRow>());

    public Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsInRangeAsync(IEnumerable<Guid> accountIds, LocalDate startDate, LocalDate endDate, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<AccountBalanceTransactionRow>());

    public Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryForReportAsync(string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<CategoryExpenseRow>());

    public Task<List<CategorySpendingRow>> GetCategorySpendingAsync(string userId, LocalDate startDate, LocalDate endDate, CancellationToken cancellationToken = default)
        => Task.FromResult(new List<CategorySpendingRow>());

    public Task<Dictionary<Guid, decimal>> GetExpensesByCategoryAsync(string userId, LocalDate startDate, LocalDate endDate, IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
        => Task.FromResult(new Dictionary<Guid, decimal>());
}
