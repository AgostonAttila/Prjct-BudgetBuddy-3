using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.ReferenceData.Features.Categories.CreateCategory;

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
