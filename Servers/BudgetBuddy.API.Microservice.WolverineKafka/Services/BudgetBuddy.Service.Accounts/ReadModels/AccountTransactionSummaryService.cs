using BudgetBuddy.Shared.Messages.Contracts.Accounts;

namespace BudgetBuddy.Service.Accounts.ReadModels;

public class AccountTransactionSummaryService(
    IAccountTransactionTotalsRepository repository) : IAccountTransactionSummary
{
    public async Task<List<AccountTransactionAggregate>> GetAggregatesForAccountsAsync(
        IEnumerable<Guid> accountIds,
        LocalDate? upToDate = null,
        CancellationToken cancellationToken = default)
    {
        // upToDate filter not supported in Fázis 2 — running totals only
        var totals = await repository.FindByIdsAsync(accountIds, cancellationToken);
        return totals
            .Select(t => new AccountTransactionAggregate(t.AccountId, t.TotalIncome, t.TotalExpense))
            .ToList();
    }

    public async Task<SingleAccountTransactionAggregate> GetAggregateForAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var totals = await repository.FindAsync(accountId, cancellationToken);
        return totals is null
            ? new SingleAccountTransactionAggregate(0, 0, 0)
            : new SingleAccountTransactionAggregate(
                totals.TotalIncome, totals.TotalExpense, totals.TransactionCount);
    }
}
