namespace BudgetBuddy.Module.Transactions.Features.Transfers.Services;

public class TransferService(
    TransactionsDbContext context,
    IAccountOwnershipService accountOwnershipService,
    ILogger<TransferService> logger) : ITransferService
{
    public async Task<(Guid FromTransactionId, Guid ToTransactionId)> CreateTransferAsync(
        string userId,
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string currencyCode,
        LocalDate transferDate,
        PaymentType paymentType,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        // Validate both accounts exist and belong to user
        var fromAccount = await accountOwnershipService.GetAccountInfoAsync(fromAccountId, userId, cancellationToken);
        if (fromAccount == null)
            throw new NotFoundException("Account", fromAccountId);

        var toAccount = await accountOwnershipService.GetAccountInfoAsync(toAccountId, userId, cancellationToken);
        if (toAccount == null)
            throw new NotFoundException("Account", toAccountId);

        // Create FROM transaction (Expense from source account)
        var fromTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = fromAccountId,
            Amount = amount,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            TransactionType = TransactionType.Transfer,
            PaymentType = paymentType,
            Note = note ?? $"Transfer to {toAccount.Name}",
            TransactionDate = transferDate,
            IsTransfer = true,
            TransferToAccountId = toAccountId,
            UserId = userId
        };

        // Create TO transaction (Income to destination account)
        var toTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = toAccountId,
            Amount = amount,
            CurrencyCode = currencyCode.ToUpperInvariant(),
            TransactionType = TransactionType.Transfer,   
            PaymentType = paymentType,
            Note = note ?? $"Transfer from {fromAccount.Name}",
            TransactionDate = transferDate,
            IsTransfer = true,
            TransferToAccountId = fromAccountId, // Link back to source
            UserId = userId
        };


        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await context.Transactions.AddRangeAsync(new[] { fromTransaction, toTransaction }, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                logger.LogInformation(
                    "Transfer created: {Amount} {Currency} from {FromAccount} to {ToAccount}",
                    amount,
                    currencyCode,
                    fromAccount.Name,
                    toAccount.Name);

                return (fromTransaction.Id, toTransaction.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Transfer failed: {Amount} {Currency} from {FromAccountId} to {ToAccountId}. Rolling back transaction.",
                    amount,
                    currencyCode,
                    fromAccountId,
                    toAccountId);

                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}
