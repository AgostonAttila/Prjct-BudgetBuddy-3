using BudgetBuddy.Shared.Contracts.Transactions;
using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.Budgets.Features.GetBudgetVsActual;

public class GetBudgetVsActualHandler(
    BudgetsDbContext context,
    ITransactionQueryService transactionQueryService,
    ICurrentUserService currentUserService,
    ILogger<GetBudgetVsActualHandler> logger) : UserAwareHandler<GetBudgetVsActualQuery, BudgetVsActualResponse>(currentUserService)
{
    public override async Task<BudgetVsActualResponse> Handle(
        GetBudgetVsActualQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Getting budget vs actual for user {UserId}, year {Year}, month {Month}",
            UserId,
            request.Year,
            request.Month);

        var startDate = new LocalDate(request.Year, request.Month, 1);
        var endDate = startDate.PlusMonths(1).PlusDays(-1);

        var budgetData = await context.Budgets
            .AsNoTracking()
            .Where(b => b.UserId == UserId && b.Year == request.Year && b.Month == request.Month)
            .Select(b => new
            {
                b.CategoryId,
                CategoryName = string.Empty,
                BudgetAmount = b.Amount
            })
            .ToListAsync(cancellationToken);

        if (budgetData.Count == 0)
        {
            return new BudgetVsActualResponse(
                request.Year,
                request.Month,
                0, 0, 0, 0,
                new List<CategoryBudgetVsActual>()
            );
        }

        var categoryIds = budgetData.Select(b => b.CategoryId).ToList();
        var actualSpending = await transactionQueryService
            .GetExpensesByCategoryAsync(UserId, startDate, endDate, categoryIds, cancellationToken);

        var categoryResults = budgetData
            .Select(b =>
            {
                var actualAmount = actualSpending.GetValueOrDefault(b.CategoryId, 0);
                var remaining = b.BudgetAmount - actualAmount;
                var utilizationPercentage = b.BudgetAmount > 0 ? (actualAmount / b.BudgetAmount) * 100 : 0;
                var isOverBudget = actualAmount > b.BudgetAmount;

                return new CategoryBudgetVsActual(
                    b.CategoryId,
                    b.CategoryName,
                    b.BudgetAmount,
                    actualAmount,
                    remaining,
                    utilizationPercentage,
                    isOverBudget
                );
            })
            .OrderByDescending(c => c.UtilizationPercentage)
            .ToList();

        var totalBudget = budgetData.Sum(b => b.BudgetAmount);
        var totalActual = actualSpending.Values.Sum();
        var totalRemaining = totalBudget - totalActual;
        var totalUtilization = totalBudget > 0 ? (totalActual / totalBudget) * 100 : 0;

        return new BudgetVsActualResponse(
            request.Year,
            request.Month,
            totalBudget,
            totalActual,
            totalRemaining,
            totalUtilization,
            categoryResults
        );
    }
}
