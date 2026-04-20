using FluentValidation;

namespace BudgetBuddy.Module.Investments.Features.UpdateInvestment;

public class UpdateInvestmentValidator : AbstractValidator<UpdateInvestmentCommand>
{
    public UpdateInvestmentValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Investment ID is required");

        RuleFor(x => x.Symbol)
            .NotEmpty()
            .WithMessage("Symbol is required")
            .MaximumLength(20)
            .WithMessage("Symbol cannot exceed 20 characters");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Investment name is required")
            .MaximumLength(200)
            .WithMessage("Investment name cannot exceed 200 characters");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");

        RuleFor(x => x.PurchasePrice)
            .GreaterThan(0)
            .WithMessage("Purchase price must be greater than 0");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .WithMessage("Currency code is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters");

        RuleFor(x => x.PurchaseDate)
            .NotEmpty()
            .WithMessage("Purchase date is required");
    }
}
