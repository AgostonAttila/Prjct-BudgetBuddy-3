using BudgetBuddy.API.VSA.Features.CategoryTypes.GetCategoryTypes;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.CategoryTypes;

public class GetCategoryTypesValidatorTests
{
    private readonly GetCategoryTypesValidator _validator = new();

    [Fact]
    public void Validate_WhenPageIsZero_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetCategoryTypesQuery(null, Page: 0));
        result.ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Fact]
    public void Validate_WhenPageSizeIsZero_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetCategoryTypesQuery(null, PageSize: 0));
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_WhenPageSizeExceedsMax_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetCategoryTypesQuery(null, PageSize: 101));
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetCategoryTypesQuery(null));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
