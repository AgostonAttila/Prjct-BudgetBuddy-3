using BudgetBuddy.API.VSA.Features.CategoryTypes.CreateCategoryType;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.CategoryTypes;

public class CreateCategoryTypeValidatorTests
{
    private readonly CreateCategoryTypeValidator _validator = new();

    [Fact]
    public void Validate_WhenCategoryIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateCategoryTypeCommand(Guid.Empty, "Sub", null, null));
        result.ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateCategoryTypeCommand(Guid.NewGuid(), "", null, null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateCategoryTypeCommand(Guid.NewGuid(), new string('a', 101), null, null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new CreateCategoryTypeCommand(Guid.NewGuid(), "Fast Food", "🍔", "#FF0000"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
