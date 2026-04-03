using BudgetBuddy.API.VSA.Common.Domain.Enums;
using BudgetBuddy.API.VSA.Features.Transfers.CreateTransfer;
using FluentValidation.TestHelper;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Transfers;

public class CreateTransferValidatorTests
{
    private readonly CreateTransferValidator _validator = new();

    private static CreateTransferCommand ValidCommand()
        => new(Guid.NewGuid(), Guid.NewGuid(), 100m, "USD", PaymentType.BankTransfer, null, new LocalDate(2024, 6, 15));

    [Fact]
    public void Validate_WhenFromAccountIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(ValidCommand() with { FromAccountId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(x => x.FromAccountId);
    }

    [Fact]
    public void Validate_WhenToAccountIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(ValidCommand() with { ToAccountId = Guid.Empty });
        result.ShouldHaveValidationErrorFor(x => x.ToAccountId);
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
        var result = _validator.TestValidate(ValidCommand() with { CurrencyCode = "US" });
        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
