using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Service.ReferenceData.Features.Categories.DeleteCategory;

public record DeleteCategoryCommand(
    Guid Id
) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Categories, Tags.SpendingByCategory, Tags.Transactions, Tags.BudgetsList, Tags.BudgetVsActual, Tags.Dashboard];
}
