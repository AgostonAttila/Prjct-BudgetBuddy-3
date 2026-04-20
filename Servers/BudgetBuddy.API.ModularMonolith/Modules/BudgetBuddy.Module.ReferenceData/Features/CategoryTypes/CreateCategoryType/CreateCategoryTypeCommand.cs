using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.ReferenceData.Features.CategoryTypes.CreateCategoryType;

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
