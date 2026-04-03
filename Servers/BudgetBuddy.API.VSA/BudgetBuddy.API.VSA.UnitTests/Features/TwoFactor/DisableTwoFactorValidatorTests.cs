using BudgetBuddy.API.VSA.Features.TwoFactor.DisableTwoFactor;
using FluentValidation.TestHelper;
using System.Security.Claims;

namespace BudgetBuddy.API.VSA.UnitTests.Features.TwoFactor;

public class DisableTwoFactorValidatorTests
{
    private readonly DisableTwoFactorValidator _validator = new();

    [Fact]
    public void Validate_WhenPasswordIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new DisableTwoFactorCommand(new ClaimsPrincipal(), ""));
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WhenPasswordIsProvided_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new DisableTwoFactorCommand(new ClaimsPrincipal(), "myPassword123"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
