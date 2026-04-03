using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Features.Investments.CreateInvestment;
using FluentValidation.TestHelper;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class CreateInvestmentValidatorTests
{
    private readonly CreateInvestmentValidator _validator = new();

    [Fact]
    public void Validate_WhenSymbolIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateInvestmentCommand("", "Apple", InvestmentType.Stock, 10, 150, "USD", new LocalDate(2024, 1, 1), null, null));
        result.ShouldHaveValidationErrorFor(x => x.Symbol);
    }

    [Fact]
    public void Validate_WhenQuantityIsZero_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateInvestmentCommand("AAPL", "Apple", InvestmentType.Stock, 0, 150, "USD", new LocalDate(2024, 1, 1), null, null));
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_WhenCurrencyCodeIsInvalid_ShouldHaveError()
    {
        var result = _validator.TestValidate(new CreateInvestmentCommand("AAPL", "Apple", InvestmentType.Stock, 10, 150, "US", new LocalDate(2024, 1, 1), null, null));
        result.ShouldHaveValidationErrorFor(x => x.CurrencyCode);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new CreateInvestmentCommand("AAPL", "Apple Inc", InvestmentType.Stock, 10, 150, "USD", new LocalDate(2024, 1, 1), null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
