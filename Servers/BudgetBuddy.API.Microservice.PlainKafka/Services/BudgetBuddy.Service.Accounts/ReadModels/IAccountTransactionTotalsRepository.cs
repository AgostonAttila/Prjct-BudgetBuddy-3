namespace BudgetBuddy.Service.Accounts.ReadModels;

public interface IAccountTransactionTotalsRepository
{
    Task<AccountTransactionTotals?> FindAsync(Guid accountId, CancellationToken ct);
    Task<List<AccountTransactionTotals>> FindByIdsAsync(IEnumerable<Guid> accountIds, CancellationToken ct);
    Task ApplyDeltaAsync(Guid accountId, decimal incomeDelta, decimal expenseDelta, int countDelta, Guid messageId, CancellationToken ct);
}
