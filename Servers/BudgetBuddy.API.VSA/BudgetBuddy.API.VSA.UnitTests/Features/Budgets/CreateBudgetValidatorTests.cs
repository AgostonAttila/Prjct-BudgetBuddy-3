using BudgetBuddy.API.VSA.Features.Budgets.CreateBudget;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Budgets;

public class CreateBudgetValidatorTests
{
    private readonly CreateBudgetValidator _validator = new();

    private static CreateBudgetCommand ValidCommand() =>
        new("Groceries", Guid.NewGuid(), 300m, "USD", 2024, 6);

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(ValidCommand() with { Name = "" });
        result.ShouldHaveValidationErrorFor(x => x.Name);
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
    public void Validate_WhenMonthOutOfRange_ShouldHaveError()
    {
        var result = _validator.TestValidate(ValidCommand() with { Month = 13 });
        result.ShouldHaveValidationErrorFor(x => x.Month);
    }

    [Fact]
    public void Validate_WhenYearOutOfRange_ShouldHaveError()
    {
        var result = _validator.TestValidate(ValidCommand() with { Year = 1999 });
        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
