using BudgetBuddy.API.VSA.Features.Transactions.BatchUpdateTransactions;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class BatchUpdateTransactionsValidatorTests
{
    private readonly BatchUpdateTransactionsValidator _validator = new();

    [Fact]
    public void Validate_WhenListIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new BatchUpdateTransactionsCommand(new List<Guid>(), Guid.NewGuid(), null));
        result.ShouldHaveValidationErrorFor(x => x.TransactionIds);
    }

    [Fact]
    public void Validate_WhenNeitherCategoryIdNorLabelsProvided_ShouldHaveError()
    {
        var result = _validator.TestValidate(new BatchUpdateTransactionsCommand(new List<Guid> { Guid.NewGuid() }, null, null));
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void Validate_WhenOnlyCategoryIdProvided_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new BatchUpdateTransactionsCommand(new List<Guid> { Guid.NewGuid() }, Guid.NewGuid(), null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenOnlyLabelsProvided_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new BatchUpdateTransactionsCommand(new List<Guid> { Guid.NewGuid() }, null, "vacation"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
