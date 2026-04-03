using BudgetBuddy.API.VSA.Common.Domain.Entities;
using BudgetBuddy.API.VSA.Features.Investments.UpdateInvestment;
using FluentValidation.TestHelper;
using NodaTime;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class UpdateInvestmentValidatorTests
{
    private readonly UpdateInvestmentValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateInvestmentCommand(Guid.Empty, "AAPL", "Apple", InvestmentType.Stock, 10, 150, "USD", new LocalDate(2024, 1, 1), null, null));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenSymbolIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateInvestmentCommand(Guid.NewGuid(), "", "Apple", InvestmentType.Stock, 10, 150, "USD", new LocalDate(2024, 1, 1), null, null));
        result.ShouldHaveValidationErrorFor(x => x.Symbol);
    }

    [Fact]
    public void Validate_WhenPurchasePriceIsZero_ShouldHaveError()
    {
        var result = _validator.TestValidate(new UpdateInvestmentCommand(Guid.NewGuid(), "AAPL", "Apple", InvestmentType.Stock, 10, 0, "USD", new LocalDate(2024, 1, 1), null, null));
        result.ShouldHaveValidationErrorFor(x => x.PurchasePrice);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new UpdateInvestmentCommand(Guid.NewGuid(), "AAPL", "Apple Inc", InvestmentType.Stock, 10, 150, "USD", new LocalDate(2024, 1, 1), null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
