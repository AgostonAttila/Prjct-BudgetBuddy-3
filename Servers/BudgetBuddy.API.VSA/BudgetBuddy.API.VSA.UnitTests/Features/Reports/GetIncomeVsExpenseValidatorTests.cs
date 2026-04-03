using BudgetBuddy.API.VSA.Features.Reports.GetIncomeVsExpense;
using FluentValidation.TestHelper;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Reports;

public class GetIncomeVsExpenseValidatorTests
{
    private readonly GetIncomeVsExpenseValidator _validator = new();

    [Fact]
    public void Validate_WhenStartDateAfterEndDate_ShouldHaveError()
    {
        var query = new GetIncomeVsExpenseQuery(
            StartDate: new LocalDate(2024, 12, 31),
            EndDate: new LocalDate(2024, 1, 1));

        var result = _validator.TestValidate(query);

        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Validate_WhenNoDatesProvided_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetIncomeVsExpenseQuery());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenStartDateBeforeEndDate_ShouldNotHaveErrors()
    {
        var query = new GetIncomeVsExpenseQuery(
            StartDate: new LocalDate(2024, 1, 1),
            EndDate: new LocalDate(2024, 6, 30));

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
