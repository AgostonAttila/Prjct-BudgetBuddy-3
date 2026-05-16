using BudgetBuddy.Service.Budgets.Features.CreateBudget;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Validators;

public class CreateBudgetValidatorTests
{
    private readonly CreateBudgetValidator _sut = new();

    private static CreateBudgetCommand Valid() => new(
        Name: "Food",
        CategoryId: Guid.NewGuid(),
        Amount: 50000m,
        CurrencyCode: "HUF",
        Year: 2024,
        Month: 6
    );

    [Fact]
    public void Valid_command_passes()
    {
        _sut.Validate(Valid()).IsValid.Should().BeTrue();
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

    [Fact]
    public void Empty_category_id_fails()
    {
        var result = _sut.Validate(Valid() with { CategoryId = Guid.Empty });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CategoryId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_amount_fails(decimal amount)
    {
        var result = _sut.Validate(Valid() with { Amount = amount });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public void Out_of_range_year_fails(int year)
    {
        var result = _sut.Validate(Valid() with { Year = year });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Year");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Out_of_range_month_fails(int month)
    {
        var result = _sut.Validate(Valid() with { Month = month });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Month");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void Valid_month_passes(int month)
    {
        _sut.Validate(Valid() with { Month = month }).IsValid.Should().BeTrue();
    }
}
