using BudgetBuddy.Service.Transactions.Features.CreateTransaction;
using BudgetBuddy.Shared.Kernel.Enums;
using FluentAssertions;
using NodaTime;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Validators;

public class CreateTransactionValidatorTests
{
    private readonly CreateTransactionValidator _sut = new();

    private static CreateTransactionCommand Valid() => new(
        AccountId: Guid.NewGuid(),
        CategoryId: null,
        TypeId: null,
        Amount: 100m,
        CurrencyCode: "HUF",
        RefCurrencyAmount: null,
        TransactionType: TransactionType.Expense,
        PaymentType: PaymentType.Card,
        Note: null,
        TransactionDate: new LocalDate(2024, 1, 15),
        IsTransfer: false,
        TransferToAccountId: null,
        Payee: null,
        Labels: null
    );

    [Fact]
    public void Valid_command_passes()
    {
        _sut.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Empty_account_id_fails()
    {
        var cmd = Valid() with { AccountId = Guid.Empty };
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.AccountId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Non_positive_amount_fails(decimal amount)
    {
        var cmd = Valid() with { Amount = amount };
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.Amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDA")]
    public void Invalid_currency_code_fails(string currency)
    {
        var cmd = Valid() with { CurrencyCode = currency };
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.CurrencyCode));
    }

    [Fact]
    public void Transfer_without_destination_account_fails()
    {
        var cmd = Valid() with { IsTransfer = true, TransferToAccountId = null };
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.TransferToAccountId));
    }

    [Fact]
    public void Transfer_with_destination_account_passes()
    {
        var cmd = Valid() with { IsTransfer = true, TransferToAccountId = Guid.NewGuid() };
        _sut.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Non_transfer_without_destination_passes()
    {
        var cmd = Valid() with { IsTransfer = false, TransferToAccountId = null };
        _sut.Validate(cmd).IsValid.Should().BeTrue();
    }
}
