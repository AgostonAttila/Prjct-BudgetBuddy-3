using FluentValidation;

namespace BudgetBuddy.Service.Investments.Features.Investments.SellInvestment;

public class SellInvestmentValidator : AbstractValidator<SellInvestmentCommand>
{
    public SellInvestmentValidator()
    {
        RuleFor(x => x.Symbol).NotEmpty().MaximumLength(20);
        RuleFor(x => x.QuantityToSell).GreaterThan(0).WithMessage("Quantity to sell must be positive.");
        RuleFor(x => x.SalePrice).GreaterThan(0).WithMessage("Sale price must be positive.");
    }
}
