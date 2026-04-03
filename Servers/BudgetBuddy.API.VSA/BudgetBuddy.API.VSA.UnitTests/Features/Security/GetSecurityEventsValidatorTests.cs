using BudgetBuddy.API.VSA.Features.Security.GetSecurityEvents;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Security;

public class GetSecurityEventsValidatorTests
{
    private readonly GetSecurityEventsValidator _validator = new();

    [Fact]
    public void Validate_WhenPageSizeIsZero_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetSecurityEventsQuery(PageSize: 0));
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_WhenPageSizeExceedsMax_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetSecurityEventsQuery(PageSize: 201));
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_WhenPageSizeIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetSecurityEventsQuery(PageSize: 50));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
