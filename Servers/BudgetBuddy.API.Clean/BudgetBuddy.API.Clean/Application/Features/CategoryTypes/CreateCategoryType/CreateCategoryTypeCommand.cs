using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Domain.Constants;

namespace BudgetBuddy.Application.Features.CategoryTypes.CreateCategoryType;

public record CreateCategoryTypeCommand(
    Guid CategoryId,
    string Name,
    string? Icon,
    string? Color
) : IRequest<CreateCategoryTypeResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.CategoryTypes];
}

public record CreateCategoryTypeResponse(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Icon,
    string? Color
);
