using BudgetBuddy.API.VSA.Features.Currencies.CreateCurrency;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Currencies;

public class CreateCurrencyValidatorTests
{
    private readonly CreateCurrencyValidator _validator = new();

    [Fact]
    public void Validate_WhenCodeIsNotThreeChars_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateCurrencyCommand("US", "$", "US Dollar"));
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WhenSymbolIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateCurrencyCommand("USD", "", "US Dollar"));
        result.ShouldHaveValidationErrorFor(x => x.Symbol);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateCurrencyCommand("USD", "$", ""));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new CreateCurrencyCommand("USD", "$", "US Dollar"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
