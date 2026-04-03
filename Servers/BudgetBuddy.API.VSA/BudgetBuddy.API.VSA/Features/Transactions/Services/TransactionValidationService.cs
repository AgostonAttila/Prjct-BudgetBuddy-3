namespace BudgetBuddy.API.VSA.Features.Transactions.Services;

public class TransactionValidationService(
    AppDbContext context,
    ILogger<TransactionValidationService> logger) : ITransactionValidationService
{
    public async Task ValidateAccountOwnershipAsync(Guid accountId, string userId, CancellationToken cancellationToken = default)
    {
        var exists = await context.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

        if (!exists)
            throw new NotFoundException(nameof(Account), accountId);
    }

    public async Task ValidateTransferDestinationAsync(Guid destinationAccountId, string userId, CancellationToken cancellationToken = default)
    {
        var exists = await context.Accounts
            .AnyAsync(a => a.Id == destinationAccountId && a.UserId == userId, cancellationToken);

        if (!exists)
            throw new NotFoundException(nameof(Account), destinationAccountId);
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
