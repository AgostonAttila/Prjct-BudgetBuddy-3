using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.Budgets.Features.DeleteBudget;

public record DeleteBudgetCommand(
    Guid Id
) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.BudgetsList, Tags.BudgetVsActual, Tags.BudgetAlerts, Tags.Dashboard];
}
