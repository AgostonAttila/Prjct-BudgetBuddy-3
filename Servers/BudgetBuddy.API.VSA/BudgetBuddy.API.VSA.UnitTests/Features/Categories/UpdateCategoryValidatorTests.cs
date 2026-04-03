using BudgetBuddy.API.VSA.Features.Categories.UpdateCategory;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Categories;

public class UpdateCategoryValidatorTests
{
    private readonly UpdateCategoryValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateCategoryCommand(Guid.Empty, "Food", null, null));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateCategoryCommand(Guid.NewGuid(), "", null, null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new UpdateCategoryCommand(Guid.NewGuid(), "Food", "🍕", "#FF0000"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
