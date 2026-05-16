using BudgetBuddy.Service.Accounts.Features.CreateAccount;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.Service.UnitTests.Validators;

public class CreateAccountValidatorTests
{
    private readonly CreateAccountValidator _sut = new();

    [Fact]
    public void Valid_command_passes()
    {
        var cmd = new CreateAccountCommand("My Account", "desc", "HUF", 0m);
        _sut.Validate(cmd).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null!)]
    public void Empty_name_fails(string? name)
    {
        var cmd = new CreateAccountCommand(name!, "desc", "HUF", 0m);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.Name));
    }

    [Fact]
    public void Name_exceeding_200_chars_fails()
    {
        var cmd = new CreateAccountCommand(new string('x', 201), "desc", "HUF", 0m);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.Name));
    }

    [Fact]
    public void Description_exceeding_500_chars_fails()
    {
        var cmd = new CreateAccountCommand("Name", new string('x', 501), "HUF", 0m);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.Description));
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDA")]
    [InlineData("usd")]
    [InlineData("US1")]
    public void Invalid_currency_code_fails(string currency)
    {
        var cmd = new CreateAccountCommand("Name", "", currency, 0m);
        var result = _sut.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(cmd.DefaultCurrencyCode));
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("HUF")]
    public void Valid_currency_code_passes(string currency)
    {
        var cmd = new CreateAccountCommand("Name", "", currency, 0m);
        _sut.Validate(cmd).IsValid.Should().BeTrue();
    }
}
