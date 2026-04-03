using BudgetBuddy.API.VSA.Features.Reports.GetSpendingByCategory;
using FluentValidation.TestHelper;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Reports;

public class GetSpendingByCategoryValidatorTests
{
    private readonly GetSpendingByCategoryValidator _validator = new();

    [Fact]
    public void Validate_WhenStartDateAfterEndDate_ShouldHaveError()
    {
        var query = new GetSpendingByCategoryQuery(
            StartDate: new LocalDate(2024, 6, 30),
            EndDate: new LocalDate(2024, 1, 1));

        var result = _validator.TestValidate(query);

        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Validate_WhenNoDatesProvided_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetSpendingByCategoryQuery());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStartDateBeforeEndDate_ShouldNotHaveErrors()
    {
        var query = new GetSpendingByCategoryQuery(
            StartDate: new LocalDate(2024, 1, 1),
            EndDate: new LocalDate(2024, 6, 30));

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
