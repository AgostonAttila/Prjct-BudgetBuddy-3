using BudgetBuddy.API.VSA.Features.Accounts.CreateAccount;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Accounts;

public class CreateAccountValidatorTests
{
    private readonly CreateAccountValidator _validator = new();

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(
            new CreateAccountCommand("", "", "USD", 0));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceeds200Chars_ShouldHaveError()
    {
        var result = _validator.TestValidate(
            new CreateAccountCommand(new string('A', 201), "", "USD", 0));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenCurrencyCodeIsNotThreeChars_ShouldHaveError()
    {
        var result = _validator.TestValidate(
            new CreateAccountCommand("Savings", "", "USDD", 0));

        result.ShouldHaveValidationErrorFor(x => x.DefaultCurrencyCode);
    }

    [Fact]
    public void Validate_WhenCurrencyCodeIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(
            new CreateAccountCommand("Savings", "", "", 0));

        result.ShouldHaveValidationErrorFor(x => x.DefaultCurrencyCode);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("HUF")]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors(string currency)
    {
        var result = _validator.TestValidate(
            new CreateAccountCommand("Savings", "My savings", currency, 500m));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
