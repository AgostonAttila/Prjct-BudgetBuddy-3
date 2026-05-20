using FluentValidation;

namespace BudgetBuddy.Service.Investments.Features.DeleteInvestment;

public class DeleteInvestmentValidator : AbstractValidator<DeleteInvestmentCommand>
{
    public DeleteInvestmentValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Investment ID is required");
    }
}
