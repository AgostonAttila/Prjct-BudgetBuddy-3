namespace BudgetBuddy.Service.Analytics.ReadModels;

public interface IInvestmentReadModelRepository
{
    Task UpsertAsync(InvestmentReadModel model, CancellationToken ct = default);
    Task DeleteAsync(Guid investmentId, CancellationToken ct = default);
}
