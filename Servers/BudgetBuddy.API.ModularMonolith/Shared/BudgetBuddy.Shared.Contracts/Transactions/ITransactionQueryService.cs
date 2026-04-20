using NodaTime;
using BudgetBuddy.Shared.Kernel.Enums;

namespace BudgetBuddy.Shared.Contracts.Transactions;

public record TransactionMonthlySummaryRow(
    string CurrencyCode,
    TransactionType TransactionType,
    decimal TotalAmount,
    int Count);

public record TransactionMonthlyGroupRow(
    int Year,
    int Month,
    string CurrencyCode,
    TransactionType TransactionType,
    decimal TotalAmount);

public record TransactionDailySummaryRow(
    int Day,
    TransactionType TransactionType,
    string CurrencyCode,
    decimal Amount,
    int Count);

public record TransactionRecentItem(
    Guid Id,
    LocalDate TransactionDate,
    Guid AccountId,
    Guid? CategoryId,
    TransactionType TransactionType,
    decimal Amount,
    string? CurrencyCode);

public record CategoryExpenseRow(
    Guid? CategoryId,
    string CurrencyCode,
    decimal Amount,
    int Count);

public record CategorySpendingRow(
    Guid CategoryId,
    string CurrencyCode,
    decimal Total);

public record AccountBalanceTransactionRow(
    Guid AccountId,
    TransactionType TransactionType,
    decimal Total);

public record TransactionCurrencyTypeTotals(
    string CurrencyCode,
    TransactionType TransactionType,
    decimal TotalAmount,
    int Count);

public interface ITransactionQueryService
{
    // Dashboard – GetMonthSummaryAsync
    Task<List<TransactionMonthlySummaryRow>> GetMonthlyTotalsAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default);

    // Dashboard – GetBudgetSummaryAsync (expenses for given categories)
    Task<List<CategorySpendingRow>> GetExpensesByCategoryIdsAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default);

    // Dashboard – GetRecentTransactionsAsync
    Task<List<TransactionRecentItem>> GetRecentTransactionsAsync(
        string userId, int count,
        CancellationToken cancellationToken = default);

    // Dashboard – GetTopCategoriesAsync
    Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default);

    // Report – CalculateIncomeVsExpenseAsync
    Task<List<TransactionCurrencyTypeTotals>> GetTotalsByCurrencyAndTypeAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default);

    // Report – GetMonthlyBreakdownAsync
    Task<List<TransactionMonthlyGroupRow>> GetGroupedByMonthCurrencyTypeAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default);

    // Report – CalculateMonthlySummaryAsync (summary sor)
    Task<List<TransactionMonthlySummaryRow>> GetMonthlySummaryGroupedAsync(
        string userId, LocalDate startDate, LocalDate endDate, Guid? accountId,
        CancellationToken cancellationToken = default);

    // Report – CalculateMonthlySummaryAsync (daily breakdown)
    Task<List<TransactionDailySummaryRow>> GetDailyBreakdownAsync(
        string userId, LocalDate startDate, LocalDate endDate, Guid? accountId,
        CancellationToken cancellationToken = default);

    // Report – CalculateMonthBalanceAsync (transactions before the month, per account)
    Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsBeforeDateAsync(
        IEnumerable<Guid> accountIds, LocalDate beforeDate,
        CancellationToken cancellationToken = default);

    // Report – CalculateMonthBalanceAsync (transactions within the month, per account)
    Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsInRangeAsync(
        IEnumerable<Guid> accountIds, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default);

    // Report – CalculateSpendingByCategoryAsync
    Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryForReportAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default);

    // BudgetAlertService – GetCategorySpendingRawAsync
    Task<List<CategorySpendingRow>> GetCategorySpendingAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default);

    // GetBudgetVsActualHandler – actualSpending
    Task<Dictionary<Guid, decimal>> GetExpensesByCategoryAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default);

    // MarketDataBackfillService – earliest transaction date
    Task<LocalDate?> GetEarliestTransactionDateAsync(
        CancellationToken cancellationToken = default);
}
