using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.Budgets.Features.UpdateBudget;

public record UpdateBudgetCommand(
    Guid Id,
    string Name,
    decimal Amount
) : IRequest<BudgetResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.BudgetsList, Tags.BudgetVsActual, Tags.BudgetAlerts, Tags.Dashboard];
}

public record BudgetResponse(
    Guid Id,
    string Name,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string CurrencyCode,
    int Year,
    int Month
);
