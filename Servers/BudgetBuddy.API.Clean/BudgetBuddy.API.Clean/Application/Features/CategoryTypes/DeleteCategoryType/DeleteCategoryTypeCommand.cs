using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Domain.Constants;

namespace BudgetBuddy.Application.Features.CategoryTypes.DeleteCategoryType;

public record DeleteCategoryTypeCommand(Guid Id) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.CategoryTypes, Tags.Categories];
}
