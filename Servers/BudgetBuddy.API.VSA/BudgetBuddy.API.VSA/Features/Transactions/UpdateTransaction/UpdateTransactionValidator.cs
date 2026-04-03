using FluentValidation;

namespace BudgetBuddy.API.VSA.Features.Transactions.UpdateTransaction;

public class UpdateTransactionValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Transaction ID is required");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .WithMessage("Currency code is required")
            .Length(3)
            .WithMessage("Currency code must be exactly 3 characters");

        RuleFor(x => x.TransactionDate)
            .NotEmpty()
            .WithMessage("Transaction date is required");
    }
}
