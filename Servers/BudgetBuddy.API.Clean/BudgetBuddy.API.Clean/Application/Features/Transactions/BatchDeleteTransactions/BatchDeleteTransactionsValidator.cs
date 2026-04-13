using FluentValidation;

namespace BudgetBuddy.Application.Features.Transactions.BatchDeleteTransactions;

public class BatchDeleteTransactionsValidator : AbstractValidator<BatchDeleteTransactionsCommand>
{
    public BatchDeleteTransactionsValidator()
    {
        RuleFor(x => x.TransactionIds)
            .NotEmpty()
            .WithMessage("Transaction IDs list cannot be empty");

        RuleFor(x => x.TransactionIds)
            .Must(ids => ids.Count <= 100)
            .WithMessage("Cannot delete more than 100 transactions at once");

        RuleForEach(x => x.TransactionIds)
            .NotEmpty()
            .WithMessage("Transaction ID cannot be empty");
    }
}
