namespace BudgetBuddy.Application.Features.Transactions.Services;

public interface ITransactionValidationService
{
    /// <summary>Throws NotFoundException if the account doesn't belong to the user.</summary>
    Task ValidateAccountOwnershipAsync(Guid accountId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Throws NotFoundException if the transfer destination account doesn't belong to the user.</summary>
    Task ValidateTransferDestinationAsync(Guid destinationAccountId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Logs a warning if a similar transaction already exists (non-blocking).</summary>
    Task WarnIfDuplicateAsync(Guid accountId, decimal amount, LocalDate date, Guid? categoryId, string userId, CancellationToken cancellationToken = default);
}
