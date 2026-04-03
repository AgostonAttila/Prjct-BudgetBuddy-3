using BudgetBuddy.API.VSA.Features.Categories.DeleteCategory;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Categories;

public class DeleteCategoryValidatorTests
{
    private readonly DeleteCategoryValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new DeleteCategoryCommand(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new DeleteCategoryCommand(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
