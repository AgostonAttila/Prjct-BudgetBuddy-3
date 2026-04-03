using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Features.Transactions.CreateTransaction;
using FluentValidation.TestHelper;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transactions;

public class CreateTransactionValidatorTests
{
    private readonly CreateTransactionValidator _validator = new();

    private static CreateTransactionCommand ValidCommand(Guid? accountId = null)
        => new(accountId ?? Guid.NewGuid(), null, null, 100m, "USD", null, TransactionType.Expense, PaymentType.Cash, null, new LocalDate(2024, 6, 15), false, null, null, null);

    [Fact]
    public void Validate_WhenAccountIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(ValidCommand(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.AccountId);
    }

    [Fact]
    public void Validate_WhenAmountIsZero_ShouldHaveError()
    {
        var cmd = ValidCommand() with { Amount = 0 };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Fact]
    public void Validate_WhenCurrencyCodeIsInvalid_ShouldHaveError()
    {
        var cmd = ValidCommand() with { CurrencyCode = "US" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }

    [Fact]
    public void Validate_WhenIsTransferButNoDestinationAccount_ShouldHaveError()
    {
        var cmd = ValidCommand() with { IsTransfer = true, TransferToAccountId = null };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.TransferToAccountId);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
