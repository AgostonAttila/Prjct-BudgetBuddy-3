namespace BudgetBuddy.Shared.Contracts.Budgets;

public record BudgetSummaryItem(
    Guid Id,
    Guid CategoryId,
    decimal Amount,
    string CurrencyCode);

public interface IBudgetQueryService
{
    Task<List<BudgetSummaryItem>> GetBudgetsForMonthAsync(
        string userId, int year, int month,
        CancellationToken cancellationToken = default);
}
