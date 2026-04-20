using BudgetBuddy.Shared.Contracts.Budgets;

namespace BudgetBuddy.Module.Budgets.Features.Budgets.Services;

public class BudgetQueryService(BudgetsDbContext context) : IBudgetQueryService
{
    public Task<List<BudgetSummaryItem>> GetBudgetsForMonthAsync(
        string userId, int year, int month,
        CancellationToken cancellationToken = default) =>
        context.Budgets
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .Select(b => new BudgetSummaryItem(
                b.Id,
                b.CategoryId,
                b.Amount,
                (b.CurrencyCode ?? "USD").ToUpper()))
            .ToListAsync(cancellationToken);
}
