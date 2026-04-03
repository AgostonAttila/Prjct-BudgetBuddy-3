using BudgetBuddy.API.VSA.Common.Shared.Contracts;
using BudgetBuddy.API.VSA.Common.Domain.Constants;

namespace BudgetBuddy.API.VSA.Features.Categories.UpdateCategory;

public record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Icon,
    string? Color
) : IRequest<CategoryResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Categories, Tags.SpendingByCategory, Tags.Dashboard];
}

public record CategoryResponse(
    Guid Id,
    string Name,
    string? Icon,
    string? Color
);
