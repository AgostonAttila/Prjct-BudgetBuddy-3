using FluentValidation;

namespace BudgetBuddy.Application.Features.Investments.DeleteInvestment;

public class DeleteInvestmentValidator : AbstractValidator<DeleteInvestmentCommand>
{
    public DeleteInvestmentValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Investment ID is required");
    }
}
