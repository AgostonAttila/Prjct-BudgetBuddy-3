using BudgetBuddy.API.VSA.Features.CategoryTypes.UpdateCategoryType;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.CategoryTypes;

public class UpdateCategoryTypeValidatorTests
{
    private readonly UpdateCategoryTypeValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateCategoryTypeCommand(Guid.Empty, "Name", null, null));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateCategoryTypeCommand(Guid.NewGuid(), "", null, null));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new UpdateCategoryTypeCommand(Guid.NewGuid(), "Fast Food", "🍔", "#FF0000"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
