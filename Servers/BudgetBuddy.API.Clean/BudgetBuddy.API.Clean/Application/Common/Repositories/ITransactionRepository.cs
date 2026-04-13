using BudgetBuddy.Application.Common.Contracts;

namespace BudgetBuddy.Application.Common.Repositories;

public record TransactionBalanceAggregate(Guid AccountId, TransactionType Type, decimal Total);

public record TransactionPageItem(
    Guid Id,
    string AccountName,
    string? CategoryName,
    decimal Amount,
    string CurrencyCode,
    TransactionType TransactionType,
    PaymentType PaymentType,
    LocalDate TransactionDate,
    bool IsTransfer);

public interface ITransactionRepository : IUserOwnedRepository<Transaction>
{
    Task<(List<TransactionPageItem> Items, int TotalCount)> GetPagedAsync(
        TransactionFilter filter, CancellationToken ct = default);
    Task<List<Transaction>> GetByIdsAsync(
        IEnumerable<Guid> ids, string userId, CancellationToken ct = default);
    Task<List<Transaction>> GetForExportAsync(
        ExportTransactionFilter filter, CancellationToken ct = default);
    Task<Dictionary<Guid, decimal>> GetSpendingByCategoryAsync(
        string userId, IList<Guid> categoryIds, LocalDate startDate, LocalDate endDate, CancellationToken ct = default);
    Task<(decimal TotalIncome, decimal TotalExpense, int TransactionCount)> GetAccountBalanceSummaryAsync(
        Guid accountId, CancellationToken ct = default);
    Task<List<TransactionBalanceAggregate>> GetBalanceAggregatesByAccountAsync(
        IList<Guid> accountIds, LocalDate? upToDate, CancellationToken ct = default);
    Task<BatchDeleteResult> BatchDeleteAsync(
        List<Guid> ids, string userId, string entityName, CancellationToken ct = default);
}
