using BudgetBuddy.Service.Investments.Features.CreateInvestment;
using BudgetBuddy.Shared.Kernel.Enums;
using FluentAssertions;
using NodaTime;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Validators;

public class CreateInvestmentValidatorTests
{
    private readonly CreateInvestmentValidator _sut = new();

    private static CreateInvestmentCommand Valid() => new(
        Symbol: "AAPL",
        Name: "Apple Inc.",
        Type: InvestmentType.Stock,
        Quantity: 10m,
        PurchasePrice: 180m,
        CurrencyCode: "USD",
        PurchaseDate: new LocalDate(2024, 1, 15),
        Note: null,
        AccountId: null
    );

    [Fact]
    public void Valid_command_passes()
    {
        _sut.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null!)]
    public void Empty_symbol_fails(string? symbol)
    {
        var result = _sut.Validate(Valid() with { Symbol = symbol! });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Symbol");
    }

    [Fact]
    public void Symbol_exceeding_20_chars_fails()
    {
        var result = _sut.Validate(Valid() with { Symbol = new string('X', 21) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Symbol");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null!)]
    public void Empty_name_fails(string? name)
    {
        var result = _sut.Validate(Valid() with { Name = name! });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Name_exceeding_200_chars_fails()
    {
        var result = _sut.Validate(Valid() with { Name = new string('x', 201) });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_quantity_fails(decimal quantity)
    {
        var result = _sut.Validate(Valid() with { Quantity = quantity });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Quantity");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Non_positive_purchase_price_fails(decimal price)
    {
        var result = _sut.Validate(Valid() with { PurchasePrice = price });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PurchasePrice");
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDA")]
    public void Invalid_currency_code_fails(string currency)
    {
        var result = _sut.Validate(Valid() with { CurrencyCode = currency });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrencyCode");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("BTC")]
    public void Valid_currency_code_passes(string currency)
    {
        _sut.Validate(Valid() with { CurrencyCode = currency }).IsValid.Should().BeTrue();
    }
}
