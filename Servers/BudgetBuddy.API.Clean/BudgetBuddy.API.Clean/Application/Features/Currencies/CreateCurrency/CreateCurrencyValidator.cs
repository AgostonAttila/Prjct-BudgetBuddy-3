using FluentValidation;

namespace BudgetBuddy.Application.Features.Currencies.CreateCurrency;

public class CreateCurrencyValidator : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters (e.g., USD, EUR, HUF)");

        RuleFor(x => x.Symbol)
            .NotEmpty()
            .MaximumLength(5)
            .WithMessage("Symbol is required and must be less than 5 characters");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Name is required and must be less than 100 characters");
    }
}
