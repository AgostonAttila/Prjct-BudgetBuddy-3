using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Domain.Constants;

namespace BudgetBuddy.API.VSA.Features.CategoryTypes.UpdateCategoryType;

public record UpdateCategoryTypeRequest(
    string Name,
    string? Icon,
    string? Color
);

public record UpdateCategoryTypeCommand(
    Guid Id,
    string Name,
    string? Icon,
    string? Color
) : IRequest<Unit>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.CategoryTypes];
}
