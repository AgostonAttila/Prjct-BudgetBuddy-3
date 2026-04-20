using FluentValidation;

namespace BudgetBuddy.Module.Transactions.Features.BatchUpdateTransactions;

public class BatchUpdateTransactionsValidator : AbstractValidator<BatchUpdateTransactionsCommand>
{
    public BatchUpdateTransactionsValidator()
    {
        RuleFor(x => x.TransactionIds)
            .NotEmpty()
            .WithMessage("Transaction IDs list cannot be empty");

        RuleFor(x => x.TransactionIds)
            .Must(ids => ids.Count <= 100)
            .WithMessage("Cannot update more than 100 transactions at once");

        RuleFor(x => x)
            .Must(x => x.CategoryId.HasValue || !string.IsNullOrWhiteSpace(x.Labels))
            .WithMessage("At least one field (CategoryId or Labels) must be provided for update");

        RuleForEach(x => x.TransactionIds)
            .NotEmpty()
            .WithMessage("Transaction ID cannot be empty");
    }
}
