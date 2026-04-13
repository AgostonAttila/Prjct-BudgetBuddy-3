using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Budgets.GetBudgetVsActual;

public class GetBudgetVsActualHandler(
    IBudgetRepository budgetRepo,
    ITransactionRepository transactionRepo,
    ICurrentUserService currentUserService,
    ILogger<GetBudgetVsActualHandler> logger) : UserAwareHandler<GetBudgetVsActualQuery, BudgetVsActualResponse>(currentUserService)
{
    public override async Task<BudgetVsActualResponse> Handle(
        GetBudgetVsActualQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Getting budget vs actual for user {UserId}, year {Year}, month {Month}",
            UserId, request.Year, request.Month);

        var budgetData = await budgetRepo.GetForVsActualAsync(UserId, request.Year, request.Month, cancellationToken);

        if (budgetData.Count == 0)
        {
            return new BudgetVsActualResponse(
                request.Year, request.Month,
                0, 0, 0, 0,
                new List<CategoryBudgetVsActual>()
            );
        }

        var startDate = new LocalDate(request.Year, request.Month, 1);
        var endDate = startDate.PlusMonths(1).PlusDays(-1);

        var categoryIds = budgetData.Select(b => b.CategoryId).ToList();
        var actualSpending = await transactionRepo.GetSpendingByCategoryAsync(
            UserId, categoryIds, startDate, endDate, cancellationToken);

        var categoryResults = budgetData
            .Select(b =>
            {
                var actualAmount = actualSpending.GetValueOrDefault(b.CategoryId, 0);
                var remaining = b.Amount - actualAmount;
                var utilizationPercentage = b.Amount > 0 ? (actualAmount / b.Amount) * 100 : 0;
                var isOverBudget = actualAmount > b.Amount;

                return new CategoryBudgetVsActual(
                    b.CategoryId, b.CategoryName,
                    b.Amount, actualAmount, remaining, utilizationPercentage, isOverBudget
                );
            })
            .OrderByDescending(c => c.UtilizationPercentage)
            .ToList();

        var totalBudget = budgetData.Sum(b => b.Amount);
        var totalActual = actualSpending.Values.Sum();
        var totalRemaining = totalBudget - totalActual;
        var totalUtilization = totalBudget > 0 ? (totalActual / totalBudget) * 100 : 0;

        return new BudgetVsActualResponse(
            request.Year, request.Month,
            totalBudget, totalActual, totalRemaining, totalUtilization,
            categoryResults
        );
    }
}
