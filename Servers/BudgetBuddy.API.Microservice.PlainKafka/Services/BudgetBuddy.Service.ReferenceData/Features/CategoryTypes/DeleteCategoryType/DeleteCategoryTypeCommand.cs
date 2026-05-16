using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Service.ReferenceData.Features.CategoryTypes.DeleteCategoryType;

public record DeleteCategoryTypeCommand(Guid Id) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.CategoryTypes, Tags.Categories];
}
