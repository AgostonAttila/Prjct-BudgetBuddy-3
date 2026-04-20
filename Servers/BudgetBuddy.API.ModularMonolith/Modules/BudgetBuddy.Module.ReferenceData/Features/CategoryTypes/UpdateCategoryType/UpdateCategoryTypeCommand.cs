using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.ReferenceData.Features.CategoryTypes.UpdateCategoryType;

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
