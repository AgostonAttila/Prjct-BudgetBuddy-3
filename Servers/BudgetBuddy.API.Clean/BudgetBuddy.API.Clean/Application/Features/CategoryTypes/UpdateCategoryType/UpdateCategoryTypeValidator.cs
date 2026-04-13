using FluentValidation;

namespace BudgetBuddy.Application.Features.CategoryTypes.UpdateCategoryType;

public class UpdateCategoryTypeValidator : AbstractValidator<UpdateCategoryTypeCommand>
{
    public UpdateCategoryTypeValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Icon)
            .MaximumLength(10)
            .When(x => !string.IsNullOrWhiteSpace(x.Icon))
            .WithMessage("Icon must not exceed 10 characters");

        RuleFor(x => x.Color)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Color))
            .WithMessage("Color must be a valid hex color (e.g., #FF5733)");
    }
}
