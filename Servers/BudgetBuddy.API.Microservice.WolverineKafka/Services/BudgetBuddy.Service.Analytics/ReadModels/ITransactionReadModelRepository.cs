namespace BudgetBuddy.Service.Analytics.ReadModels;

public interface ITransactionReadModelRepository
{
    Task UpsertAsync(TransactionReadModel model, CancellationToken ct = default);
    Task DeleteAsync(Guid transactionId, CancellationToken ct = default);
}
