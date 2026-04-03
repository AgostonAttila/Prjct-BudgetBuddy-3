using BudgetBuddy.API.VSA.Features.Investments.BatchDeleteInvestments;
using FluentValidation.TestHelper;

namespace BudgetBuddy.API.VSA.UnitTests.Features.Investments;

public class BatchDeleteInvestmentsValidatorTests
{
    private readonly BatchDeleteInvestmentsValidator _validator = new();

    [Fact]
    public void Validate_WhenListIsEmpty_ShouldHaveError()
    {
        var result = _validator.TestValidate(new BatchDeleteInvestmentsCommand(new List<Guid>()));
        result.ShouldHaveValidationErrorFor(x => x.InvestmentIds);
    }

    [Fact]
    public void Validate_WhenMoreThan100Ids_ShouldHaveError()
    {
        var ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();
        var result = _validator.TestValidate(new BatchDeleteInvestmentsCommand(ids));
        result.ShouldHaveValidationErrorFor(x => x.InvestmentIds);
    }

    [Fact]
    public void Validate_WhenValidIds_ShouldNotHaveErrors()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var result = _validator.TestValidate(new BatchDeleteInvestmentsCommand(ids));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
