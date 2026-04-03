using BudgetBuddy.API.VSA.Features.Reports.GetMonthlySummary;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Reports;

public class GetMonthlySummaryValidatorTests
{
    private readonly GetMonthlySummaryValidator _validator = new();

    [Fact]
    public void Validate_WhenYearIsOutOfRange_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetMonthlySummaryQuery(1800, 6));
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Validate_WhenMonthIsZero_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetMonthlySummaryQuery(2024, 0));
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Validate_WhenMonthIsThirteen_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetMonthlySummaryQuery(2024, 13));
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetMonthlySummaryQuery(2024, 6));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
