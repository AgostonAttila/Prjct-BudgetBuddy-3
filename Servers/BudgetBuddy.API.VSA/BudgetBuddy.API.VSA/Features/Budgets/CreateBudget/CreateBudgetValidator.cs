using FluentValidation;

namespace BudgetBuddy.API.VSA.Features.Budgets.CreateBudget;

public class CreateBudgetValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Budget name is required")
            .MaximumLength(200)
            .WithMessage("Budget name cannot exceed 200 characters");

        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("Category ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Budget amount must be greater than 0");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .WithMessage("Currency code is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Year must be between 2000 and 2100");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12");
    }
}
