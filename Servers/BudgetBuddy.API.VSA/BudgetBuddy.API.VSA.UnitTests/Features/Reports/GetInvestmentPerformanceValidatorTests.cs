using BudgetBuddy.API.VSA.Features.Reports.GetInvestmentPerformance;
using FluentValidation.TestHelper;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Reports;

public class GetInvestmentPerformanceValidatorTests
{
    private readonly GetInvestmentPerformanceValidator _validator = new();

    [Fact]
    public void Validate_WhenStartDateAfterEndDate_ShouldHaveError()
    {
        var query = new GetInvestmentPerformanceQuery(
            StartDate: new LocalDate(2024, 12, 31),
            EndDate: new LocalDate(2024, 1, 1));

        var result = _validator.TestValidate(query);

        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Validate_WhenNoDatesProvided_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetInvestmentPerformanceQuery());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenDatesAreValid_ShouldNotHaveErrors()
    {
        var query = new GetInvestmentPerformanceQuery(
            StartDate: new LocalDate(2024, 1, 1),
            EndDate: new LocalDate(2024, 6, 30));

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
