using BudgetBuddy.API.VSA.Features.Budgets.GetBudgetVsActual;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Budgets;

public class GetBudgetVsActualValidatorTests
{
    private readonly GetBudgetVsActualValidator _validator = new();

    [Fact]
    public void Validate_WhenMonthIsInvalid_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetBudgetVsActualQuery(2024, 0));
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Validate_WhenYearIsOutOfRange_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetBudgetVsActualQuery(1800, 6));
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetBudgetVsActualQuery(2024, 6));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
