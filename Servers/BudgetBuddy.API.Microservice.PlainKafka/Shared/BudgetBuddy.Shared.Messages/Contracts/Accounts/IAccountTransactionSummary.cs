using NodaTime;

namespace BudgetBuddy.Shared.Messages.Contracts.Accounts;

public interface IAccountTransactionSummary
{
    Task<List<AccountTransactionAggregate>> GetAggregatesForAccountsAsync(
        IEnumerable<Guid> accountIds,
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default);

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
