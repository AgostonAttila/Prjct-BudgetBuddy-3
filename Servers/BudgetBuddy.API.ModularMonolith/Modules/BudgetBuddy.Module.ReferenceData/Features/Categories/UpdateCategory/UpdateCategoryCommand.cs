using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.ReferenceData.Features.Categories.UpdateCategory;

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
