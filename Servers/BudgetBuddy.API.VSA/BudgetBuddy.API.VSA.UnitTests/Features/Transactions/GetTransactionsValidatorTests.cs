using BudgetBuddy.API.VSA.Features.Transactions.GetTransactions;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class GetTransactionsValidatorTests
{
    private readonly GetTransactionsValidator _validator = new();

    [Fact]
    public void Validate_WhenPageSizeExceedsMax_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetTransactionsQuery(PageSize: 101));
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_WhenPageNumberIsZero_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetTransactionsQuery(PageNumber: 0));
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Fact]
    public void Validate_WhenSearchTermExceedsMaxLength_ShouldHaveError()
    {
        var result = _validator.TestValidate(new GetTransactionsQuery(SearchTerm: new string('a', 101)));
        result.ShouldHaveValidationErrorFor(x => x.SearchTerm);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new GetTransactionsQuery());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
