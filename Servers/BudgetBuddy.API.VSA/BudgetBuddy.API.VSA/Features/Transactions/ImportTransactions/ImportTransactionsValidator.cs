using FluentValidation;

namespace BudgetBuddy.API.VSA.Features.Transactions.ImportTransactions;

public class ImportTransactionsValidator : AbstractValidator<ImportTransactionsCommand>
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public ImportTransactionsValidator()
    {
        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("File stream cannot be null")
            .Must(stream => stream != null && stream.Length > 0)
            .WithMessage("File cannot be empty")
            .Must(stream => stream == null || stream.Length <= MaxFileSizeBytes)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / 1024 / 1024} MB");
    }
}
