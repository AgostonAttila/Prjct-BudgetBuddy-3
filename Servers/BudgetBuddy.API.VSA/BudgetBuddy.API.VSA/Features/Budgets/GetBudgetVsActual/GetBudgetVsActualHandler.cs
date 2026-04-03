using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;

namespace BudgetBuddy.API.VSA.Features.Budgets.GetBudgetVsActual;

public class GetBudgetVsActualHandler(
    AppDbContext context,
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
                CategoryName = b.Category.Name,  
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
        var actualSpending = await context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.UserId == UserId &&
                categoryIds.Contains(t.CategoryId!.Value) &&
                t.TransactionType == TransactionType.Expense &&
                t.TransactionDate >= startDate &&
                t.TransactionDate <= endDate)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key!.Value, Amount = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Amount, cancellationToken);

      
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
