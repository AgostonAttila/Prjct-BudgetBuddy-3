using BudgetBuddy.API.VSA.Features.Budgets.UpdateBudget;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Budgets;

public class UpdateBudgetValidatorTests
{
    private readonly UpdateBudgetValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateBudgetCommand(Guid.Empty, "Name", 100m));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateBudgetCommand(Guid.NewGuid(), "", 100m));
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenAmountIsZeroOrNegative_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateBudgetCommand(Guid.NewGuid(), "Name", 0));
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new UpdateBudgetCommand(Guid.NewGuid(), "Groceries", 200m));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
