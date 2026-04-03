using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Domain.Constants;

namespace BudgetBuddy.API.VSA.Features.Budgets.DeleteBudget;

public record DeleteBudgetCommand(
    Guid Id
) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.BudgetsList, Tags.BudgetVsActual, Tags.BudgetAlerts, Tags.Dashboard];
}
