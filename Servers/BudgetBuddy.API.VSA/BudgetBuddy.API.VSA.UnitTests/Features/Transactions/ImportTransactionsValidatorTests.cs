using BudgetBuddy.API.VSA.Features.Transactions.ImportTransactions;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class ImportTransactionsValidatorTests
{
    private readonly ImportTransactionsValidator _validator = new();

    [Fact]
    public void Validate_WhenStreamIsNull_ShouldHaveError()
    {
        var result = _validator.TestValidate(new ImportTransactionsCommand(null!));
        result.ShouldHaveValidationErrorFor(x => x.FileStream);
    }

    [Fact]
    public void Validate_WhenStreamIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new ImportTransactionsCommand(new MemoryStream()));
        result.ShouldHaveValidationErrorFor(x => x.FileStream);
    }

    [Fact]
    public void Validate_WhenStreamHasContent_ShouldNotHaveErrors()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = _validator.TestValidate(new ImportTransactionsCommand(stream));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
