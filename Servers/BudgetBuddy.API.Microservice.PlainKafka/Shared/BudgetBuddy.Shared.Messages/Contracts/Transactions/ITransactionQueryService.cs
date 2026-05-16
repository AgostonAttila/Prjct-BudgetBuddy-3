using BudgetBuddy.Shared.Kernel.Enums;
using NodaTime;

namespace BudgetBuddy.Shared.Messages.Contracts.Transactions;

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
    Task<List<TransactionMonthlySummaryRow>> GetMonthlyTotalsAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default);

    Task<List<CategorySpendingRow>> GetExpensesByCategoryIdsAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default);

    Task<List<TransactionRecentItem>> GetRecentTransactionsAsync(
        string userId, int count,
        CancellationToken cancellationToken = default);

    Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default);

    Task<List<TransactionCurrencyTypeTotals>> GetTotalsByCurrencyAndTypeAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default);

    Task<List<TransactionMonthlyGroupRow>> GetGroupedByMonthCurrencyTypeAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default);

    Task<List<TransactionMonthlySummaryRow>> GetMonthlySummaryGroupedAsync(
        string userId, LocalDate startDate, LocalDate endDate, Guid? accountId,
        CancellationToken cancellationToken = default);

    Task<List<TransactionDailySummaryRow>> GetDailyBreakdownAsync(
        string userId, LocalDate startDate, LocalDate endDate, Guid? accountId,
        CancellationToken cancellationToken = default);

    Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsBeforeDateAsync(
        IEnumerable<Guid> accountIds, LocalDate beforeDate,
        CancellationToken cancellationToken = default);

    Task<List<AccountBalanceTransactionRow>> GetTransactionTotalsInRangeAsync(
        IEnumerable<Guid> accountIds, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default);

    Task<List<CategoryExpenseRow>> GetExpensesGroupedByCategoryForReportAsync(
        string userId, LocalDate? startDate, LocalDate? endDate, Guid? accountId,
        CancellationToken cancellationToken = default);

    Task<List<CategorySpendingRow>> GetCategorySpendingAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, decimal>> GetExpensesByCategoryAsync(
        string userId, LocalDate startDate, LocalDate endDate,
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default);

    Task<LocalDate?> GetEarliestTransactionDateAsync(
        CancellationToken cancellationToken = default);
}
