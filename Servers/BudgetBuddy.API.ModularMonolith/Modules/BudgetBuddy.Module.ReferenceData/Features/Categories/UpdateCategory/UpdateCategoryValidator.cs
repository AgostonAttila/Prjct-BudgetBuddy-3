using FluentValidation;

namespace BudgetBuddy.Module.ReferenceData.Features.Categories.UpdateCategory;

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Category ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Category name is required")
            .MaximumLength(200)
            .WithMessage("Category name cannot exceed 200 characters");

        RuleFor(x => x.Icon)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.Icon))
            .WithMessage("Icon cannot exceed 50 characters");

        RuleFor(x => x.Color)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Color))
            .WithMessage("Color cannot exceed 20 characters");
    }
}
