using FluentValidation;

namespace BudgetBuddy.API.VSA.Features.Currencies.UpdateCurrency;

public class UpdateCurrencyValidator : AbstractValidator<UpdateCurrencyCommand>
{
    public UpdateCurrencyValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Currency ID is required");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Currency code is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters (e.g., USD, EUR)");

        RuleFor(x => x.Symbol)
            .NotEmpty()
            .WithMessage("Currency symbol is required")
            .MaximumLength(10)
            .WithMessage("Currency symbol cannot exceed 10 characters");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Currency name is required")
            .MaximumLength(100)
            .WithMessage("Currency name cannot exceed 100 characters");
    }
}
