using BudgetBuddy.API.VSA.Features.Accounts.UpdateAccount;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Accounts;

public class UpdateAccountValidatorTests
{
    private readonly UpdateAccountValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateAccountCommand(Guid.Empty, "Name", "", "USD", 0));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateAccountCommand(Guid.NewGuid(), "", "", "USD", 0));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenCurrencyCodeIsNotThreeChars_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateAccountCommand(Guid.NewGuid(), "Name", "", "USDD", 0));
        result.ShouldHaveValidationErrorFor(x => x.DefaultCurrencyCode);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new UpdateAccountCommand(Guid.NewGuid(), "Savings", "desc", "EUR", 100));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
