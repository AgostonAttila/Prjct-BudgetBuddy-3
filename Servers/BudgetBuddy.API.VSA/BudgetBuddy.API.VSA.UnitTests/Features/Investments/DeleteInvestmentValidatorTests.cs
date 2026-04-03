using BudgetBuddy.API.VSA.Features.Investments.DeleteInvestment;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class DeleteInvestmentValidatorTests
{
    private readonly DeleteInvestmentValidator _validator = new();

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new DeleteInvestmentCommand(Guid.Empty));
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_WhenIdIsValid_ShouldNotHaveErrors()
    {
        var result = _validator.TestValidate(new DeleteInvestmentCommand(Guid.NewGuid()));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
