using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Domain.Constants;

namespace BudgetBuddy.Application.Features.CategoryTypes.UpdateCategoryType;

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
