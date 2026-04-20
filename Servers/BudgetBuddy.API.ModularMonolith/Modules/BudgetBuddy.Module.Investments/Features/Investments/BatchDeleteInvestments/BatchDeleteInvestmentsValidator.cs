using FluentValidation;

namespace BudgetBuddy.Module.Investments.Features.BatchDeleteInvestments;

public class BatchDeleteInvestmentsValidator : AbstractValidator<BatchDeleteInvestmentsCommand>
{
    public BatchDeleteInvestmentsValidator()
    {
        RuleFor(x => x.InvestmentIds)
            .NotEmpty()
            .WithMessage("Investment IDs list cannot be empty");

        RuleFor(x => x.InvestmentIds)
            .Must(ids => ids.Count <= 100)
            .WithMessage("Cannot delete more than 100 investments at once");

        RuleForEach(x => x.InvestmentIds)
            .NotEmpty()
            .WithMessage("Investment ID cannot be empty");
    }
}
