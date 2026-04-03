using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Features.Transactions.UpdateTransaction;
using FluentValidation.TestHelper;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class UpdateTransactionValidatorTests
{
    private readonly UpdateTransactionValidator _validator = new();

    private static UpdateTransactionCommand ValidCommand()
        => new(Guid.NewGuid(), null, null, 100m, "USD", null, TransactionType.Expense, PaymentType.Cash, null, new LocalDate(2024, 6, 15), null, null);

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(ValidCommand() with { Id = Guid.Empty });
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenAmountIsZero_ShouldHaveError()
    {
        var result = _validator.TestValidate(ValidCommand() with { Amount = 0 });
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_WhenCurrencyCodeIsInvalid_ShouldHaveError()
    {
        var result = _validator.TestValidate(ValidCommand() with { CurrencyCode = "EURO" });
        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
