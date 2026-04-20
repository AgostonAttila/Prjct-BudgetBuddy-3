using FluentValidation;

namespace BudgetBuddy.Module.Transactions.Features.DeleteTransaction;

public class DeleteTransactionValidator : AbstractValidator<DeleteTransactionCommand>
{
    public DeleteTransactionValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Transaction ID is required");
    }
}
