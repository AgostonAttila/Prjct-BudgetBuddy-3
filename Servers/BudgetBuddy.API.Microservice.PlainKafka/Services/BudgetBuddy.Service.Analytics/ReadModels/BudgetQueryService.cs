using BudgetBuddy.Service.Analytics.Persistence;
using BudgetBuddy.Shared.Messages.Contracts.Budgets;

namespace BudgetBuddy.Service.Analytics.ReadModels;

public class BudgetQueryService(AnalyticsDbContext context) : IBudgetQueryService
{
    public Task<List<BudgetSummaryItem>> GetBudgetsForMonthAsync(
        string userId, int year, int month, CancellationToken cancellationToken = default)
        => context.Budgets
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .Select(b => new BudgetSummaryItem(b.BudgetId, b.CategoryId, b.Amount, b.CurrencyCode))
            .ToListAsync(cancellationToken);
}
