using FluentValidation;

namespace BudgetBuddy.Application.Features.Budgets.DeleteBudget;

public class DeleteBudgetValidator : AbstractValidator<DeleteBudgetCommand>
{
    public DeleteBudgetValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Budget ID is required");
    }
}
