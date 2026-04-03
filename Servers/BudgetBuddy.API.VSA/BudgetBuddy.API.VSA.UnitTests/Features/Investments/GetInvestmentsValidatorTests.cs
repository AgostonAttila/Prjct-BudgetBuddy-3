using BudgetBuddy.API.VSA.Features.Investments.GetInvestments;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class GetInvestmentsValidatorTests
{
    private readonly GetInvestmentsValidator _validator = new();

    [Fact]
    public void Validate_WhenPageNumberIsZero_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetInvestmentsQuery(PageNumber: 0));
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Fact]
    public void Validate_WhenPageSizeExceedsMax_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetInvestmentsQuery(PageSize: 101));
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_WhenSearchTermHasInvalidChars_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetInvestmentsQuery(SearchTerm: "<script>"));
        result.ShouldHaveValidationErrorFor(x => x.SearchTerm);
    }

    [Fact]
    public void Validate_WhenQueryIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetInvestmentsQuery());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
