using FluentValidation;

namespace BudgetBuddy.Application.Features.Investments.GetPortfolioValue;

public class GetPortfolioValueValidator : AbstractValidator<GetPortfolioValueQuery>
{
    public GetPortfolioValueValidator()
    {
        // Currency code validation - ISO 4217 format (3 uppercase letters)
        RuleFor(x => x.TargetCurrency)
            .NotEmpty()
            .WithMessage("Target currency is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters")
            .Matches(@"^[A-Z]{3}$")
            .WithMessage("Currency code must be 3 uppercase letters (e.g., USD, EUR, GBP)");
    }
}
