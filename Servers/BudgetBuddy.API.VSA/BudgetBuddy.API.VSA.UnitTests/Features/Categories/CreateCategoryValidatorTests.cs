using BudgetBuddy.API.VSA.Features.Categories.CreateCategory;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Categories;

public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateCategoryCommand("", null, null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateCategoryCommand(new string('a', 201), null, null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenIconExceedsMaxLength_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateCategoryCommand("Food", new string('x', 51), null));
        result.ShouldHaveValidationErrorFor(x => x.Icon);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new CreateCategoryCommand("Food", "🍕", "#FF0000"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOptionalFieldsAreNull_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new CreateCategoryCommand("Food", null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
