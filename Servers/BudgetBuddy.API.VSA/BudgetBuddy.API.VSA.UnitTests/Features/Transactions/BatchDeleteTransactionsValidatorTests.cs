using BudgetBuddy.API.VSA.Features.Transactions.BatchDeleteTransactions;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class BatchDeleteTransactionsValidatorTests
{
    private readonly BatchDeleteTransactionsValidator _validator = new();

    [Fact]
    public void Validate_WhenListIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new BatchDeleteTransactionsCommand(new List<Guid>()));
        result.ShouldHaveValidationErrorFor(x => x.TransactionIds);
    }

    [Fact]
    public void Validate_WhenListExceedsMaxCount_ShouldHaveError()
    {
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var result = _validator.TestValidate(new BatchDeleteTransactionsCommand(ids));
        result.ShouldHaveValidationErrorFor(x => x.TransactionIds);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var result = _validator.TestValidate(new BatchDeleteTransactionsCommand(ids));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
