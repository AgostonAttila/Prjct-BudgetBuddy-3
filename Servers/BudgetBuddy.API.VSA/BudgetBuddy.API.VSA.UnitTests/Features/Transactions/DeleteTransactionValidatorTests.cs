using BudgetBuddy.API.VSA.Features.Transactions.DeleteTransaction;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class DeleteTransactionValidatorTests
{
    private readonly DeleteTransactionValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new DeleteTransactionCommand(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new DeleteTransactionCommand(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
