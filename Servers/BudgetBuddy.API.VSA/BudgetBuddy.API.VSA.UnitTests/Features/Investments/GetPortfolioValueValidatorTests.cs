using BudgetBuddy.API.VSA.Features.Investments.GetPortfolioValue;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class GetPortfolioValueValidatorTests
{
    private readonly GetPortfolioValueValidator _validator = new();

    [Fact]
    public void Validate_WhenTargetCurrencyIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetPortfolioValueQuery(""));
        result.ShouldHaveValidationErrorFor(x => x.TargetCurrency);
    }

    [Fact]
    public void Validate_WhenTargetCurrencyIsLowercase_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetPortfolioValueQuery("usd"));
        result.ShouldHaveValidationErrorFor(x => x.TargetCurrency);
    }

    [Fact]
    public void Validate_WhenTargetCurrencyIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetPortfolioValueQuery("USD"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
