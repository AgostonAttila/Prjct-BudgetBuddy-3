using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Domain.Constants;

namespace BudgetBuddy.Application.Features.Categories.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string? Icon,
    string? Color
) : IRequest<CreateCategoryResponse>, ICacheInvalidator
{
    public string[] CacheTags => [Tags.Categories, Tags.SpendingByCategory, Tags.Dashboard];
}

public record CreateCategoryResponse(
    Guid Id,
    string Name,
    string? Icon,
    string? Color
);
