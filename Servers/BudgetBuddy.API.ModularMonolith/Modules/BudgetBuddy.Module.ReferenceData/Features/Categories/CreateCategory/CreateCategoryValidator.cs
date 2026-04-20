using FluentValidation;

namespace BudgetBuddy.Module.ReferenceData.Features.Categories.CreateCategory;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Category name is required and must be less than 200 characters");

        RuleFor(x => x.Icon)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.Icon))
            .WithMessage("Icon must be less than 50 characters");

        RuleFor(x => x.Color)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Color))
            .WithMessage("Color must be less than 20 characters");
    }
}
