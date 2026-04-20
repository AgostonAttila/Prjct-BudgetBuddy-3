using NodaTime;

namespace BudgetBuddy.Shared.Contracts.Accounts;

/// <summary>
/// Cross-module interface allowing the Accounts module to query transaction aggregates
/// from the Transactions module without a direct project reference.
/// Implemented by the Transactions module and registered in DI.
/// </summary>
public interface IAccountTransactionSummary
{
    /// <summary>
    /// Returns aggregated income and expense totals grouped by accountId and transaction type,
    /// optionally filtered up to a given date.
    /// </summary>
    Task<List<AccountTransactionAggregate>> GetAggregatesForAccountsAsync(
        IEnumerable<Guid> accountIds,
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns aggregated income and expense totals for a single account,
    /// with total transaction count.
    /// </summary>
    Task<SingleAccountTransactionAggregate> GetAggregateForAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);
}

public record AccountTransactionAggregate(
    Guid AccountId,
    decimal TotalIncome,
    decimal TotalExpense);

public record SingleAccountTransactionAggregate(
    decimal TotalIncome,
    decimal TotalExpense,
    int TransactionCount);
