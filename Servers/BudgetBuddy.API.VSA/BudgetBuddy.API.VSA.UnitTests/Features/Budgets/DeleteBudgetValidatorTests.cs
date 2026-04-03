using BudgetBuddy.API.VSA.Features.Budgets.DeleteBudget;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Budgets;

public class DeleteBudgetValidatorTests
{
    private readonly DeleteBudgetValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new DeleteBudgetCommand(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new DeleteBudgetCommand(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
