using BudgetBuddy.API.VSA.Features.Budgets.GetBudgets;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Budgets;

public class GetBudgetsValidatorTests
{
    private readonly GetBudgetsValidator _validator = new();

    [Fact]
    public void Validate_WhenMonthOutOfRange_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetBudgetsQuery(Month: 13));
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Validate_WhenNoFilters_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetBudgetsQuery());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenValidFilters_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetBudgetsQuery(Year: 2024, Month: 6));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
