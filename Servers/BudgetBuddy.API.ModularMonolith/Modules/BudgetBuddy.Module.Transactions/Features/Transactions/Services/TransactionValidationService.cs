using BudgetBuddy.Shared.Contracts.Accounts;

namespace BudgetBuddy.Module.Transactions.Features.Transactions.Services;

public class TransactionValidationService(
    TransactionsDbContext context,
    IAccountOwnershipService accountOwnershipService,
    ILogger<TransactionValidationService> logger) : ITransactionValidationService
{
    public async Task ValidateAccountOwnershipAsync(Guid accountId, string userId, CancellationToken cancellationToken = default)
    {
        var exists = await accountOwnershipService
            .AccountBelongsToUserAsync(accountId, userId, cancellationToken);

        if (!exists)
            throw new NotFoundException("Account", accountId);
    }

    public async Task ValidateTransferDestinationAsync(Guid destinationAccountId, string userId, CancellationToken cancellationToken = default)
    {
        var exists = await accountOwnershipService
            .AccountBelongsToUserAsync(destinationAccountId, userId, cancellationToken);

        if (!exists)
            throw new NotFoundException("Account", destinationAccountId);
    }

    public async Task WarnIfDuplicateAsync(Guid accountId, decimal amount, LocalDate date, Guid? categoryId, string userId, CancellationToken cancellationToken = default)
    {
        var hasSimilar = await context.Transactions
            .AnyAsync(t =>
                t.UserId == userId &&
                t.AccountId == accountId &&
                t.Amount == amount &&
                t.TransactionDate == date &&
                t.CategoryId == categoryId &&
                !t.IsTransfer,
                cancellationToken);

        if (hasSimilar)
            logger.LogWarning(
                "Similar transaction detected: User {UserId}, Account {AccountId}, Amount {Amount}, Date {Date}",
                userId, accountId, amount, date);
    }
}
