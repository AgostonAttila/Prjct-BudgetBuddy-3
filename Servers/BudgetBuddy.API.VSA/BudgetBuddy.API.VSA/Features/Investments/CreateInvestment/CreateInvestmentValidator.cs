using FluentValidation;

namespace BudgetBuddy.API.VSA.Features.Investments.CreateInvestment;

public class CreateInvestmentValidator : AbstractValidator<CreateInvestmentCommand>
{
    public CreateInvestmentValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("Symbol is required and must be less than 20 characters");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Name is required and must be less than 200 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.PurchasePrice)
            .GreaterThan(0)
            .WithMessage("Purchase price must be greater than 0");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters");
    }
}
