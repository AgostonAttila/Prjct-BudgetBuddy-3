namespace BudgetBuddy.Application.Features.Budgets.GetBudgetVsActual;

public record GetBudgetVsActualQuery(
    int Year,
    int Month
) : IRequest<BudgetVsActualResponse>;

public record BudgetVsActualResponse(
    int Year,
    int Month,
    decimal TotalBudget,
    decimal TotalActual,
    decimal TotalRemaining,
    decimal UtilizationPercentage,
    List<CategoryBudgetVsActual> Categories
);

public record CategoryBudgetVsActual(
    Guid CategoryId,
    string CategoryName,
    decimal BudgetAmount,
    decimal ActualAmount,
    decimal Remaining,
    decimal UtilizationPercentage,
    bool IsOverBudget
);
