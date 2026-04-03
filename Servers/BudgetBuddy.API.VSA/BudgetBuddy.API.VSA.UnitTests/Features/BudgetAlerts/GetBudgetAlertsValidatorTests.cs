using BudgetBuddy.API.VSA.Features.BudgetAlerts.GetBudgetAlerts;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.BudgetAlerts;

public class GetBudgetAlertsValidatorTests
{
    private readonly GetBudgetAlertsValidator _validator = new();

    [Fact]
    public void Validate_WhenMonthOutOfRange_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetBudgetAlertsQuery(null, 13));
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Validate_WhenMonthIsZero_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetBudgetAlertsQuery(null, 0));
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Validate_WhenNoFilters_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetBudgetAlertsQuery(null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenValidYearAndMonth_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetBudgetAlertsQuery(Year: 2024, Month: 6));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
