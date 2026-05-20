namespace BudgetBuddy.Service.Analytics.ReadModels;

public interface IBudgetReadModelRepository
{
    Task UpsertAsync(BudgetReadModel model, CancellationToken ct = default);
    Task DeleteAsync(Guid budgetId, CancellationToken ct = default);
}
