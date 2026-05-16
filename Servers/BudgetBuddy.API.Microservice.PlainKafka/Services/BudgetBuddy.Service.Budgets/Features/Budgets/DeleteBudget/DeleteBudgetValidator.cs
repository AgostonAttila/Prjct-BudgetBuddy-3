using FluentValidation;

namespace BudgetBuddy.Service.Budgets.Features.DeleteBudget;

public class DeleteBudgetValidator : AbstractValidator<DeleteBudgetCommand>
{
    public DeleteBudgetValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Budget ID is required");
    }
}
