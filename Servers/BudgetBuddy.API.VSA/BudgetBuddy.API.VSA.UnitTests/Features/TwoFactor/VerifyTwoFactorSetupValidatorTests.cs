using BudgetBuddy.API.VSA.Features.TwoFactor.VerifyTwoFactorSetup;
using FluentValidation.TestHelper;
using System.Security.Claims;

namespace BudgetBuddy.API.VSA.UnitTests.Features.TwoFactor;

public class VerifyTwoFactorSetupValidatorTests
{
    private readonly VerifyTwoFactorSetupValidator _validator = new();

    [Fact]
    public void Validate_WhenCodeIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new VerifyTwoFactorSetupCommand(new ClaimsPrincipal(), ""));
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WhenCodeIsNot6Digits_ShouldHaveError()
    {
        var result = _validator.TestValidate(new VerifyTwoFactorSetupCommand(new ClaimsPrincipal(), "1234"));
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WhenCodeContainsLetters_ShouldHaveError()
    {
        var result = _validator.TestValidate(new VerifyTwoFactorSetupCommand(new ClaimsPrincipal(), "12345a"));
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_WhenCodeIs6Digits_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new VerifyTwoFactorSetupCommand(new ClaimsPrincipal(), "123456"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
