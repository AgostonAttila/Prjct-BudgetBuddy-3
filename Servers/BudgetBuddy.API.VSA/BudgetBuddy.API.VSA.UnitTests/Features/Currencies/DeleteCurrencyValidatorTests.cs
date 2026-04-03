using BudgetBuddy.API.VSA.Features.Currencies.DeleteCurrency;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Currencies;

public class DeleteCurrencyValidatorTests
{
    private readonly DeleteCurrencyValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new DeleteCurrencyCommand(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new DeleteCurrencyCommand(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
