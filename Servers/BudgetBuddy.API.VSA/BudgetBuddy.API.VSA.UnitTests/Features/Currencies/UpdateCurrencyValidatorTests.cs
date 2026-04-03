using BudgetBuddy.API.VSA.Features.Currencies.UpdateCurrency;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Currencies;

public class UpdateCurrencyValidatorTests
{
    private readonly UpdateCurrencyValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateCurrencyCommand(Guid.Empty, "USD", "$", "US Dollar"));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenCodeIsInvalid_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateCurrencyCommand(Guid.NewGuid(), "US", "$", "US Dollar"));
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new UpdateCurrencyCommand(Guid.NewGuid(), "EUR", "€", "Euro"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
