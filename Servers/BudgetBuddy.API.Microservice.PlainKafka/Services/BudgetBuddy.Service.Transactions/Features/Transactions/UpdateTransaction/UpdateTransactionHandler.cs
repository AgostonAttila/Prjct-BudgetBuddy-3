using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Messages.Events.Transactions;

namespace BudgetBuddy.Service.Transactions.Features.UpdateTransaction;

public class UpdateTransactionHandler(
    TransactionsDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    IDomainEventCollector eventCollector,
    ILogger<UpdateTransactionHandler> logger) : UserAwareHandler<UpdateTransactionCommand, TransactionResponse>(currentUserService)
{
    public override async Task<TransactionResponse> Handle(
        UpdateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating transaction {TransactionId} for user {UserId}", request.Id, UserId);

        var transaction = await context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == UserId, cancellationToken);

        if (transaction == null)
        {
            throw new NotFoundException(nameof(Transaction), request.Id);
        }

        var oldAmount = transaction.Amount;

        mapper.Map(request, transaction);

        eventCollector.Collect(new TransactionUpdatedEvent
        {
            TransactionId = transaction.Id,
            UserId = UserId,
            AccountId = transaction.AccountId,
            CategoryId = transaction.CategoryId,
            Amount = transaction.Amount,
            OldAmount = oldAmount,
            CurrencyCode = transaction.CurrencyCode,
            TransactionType = transaction.TransactionType,
            TransactionDate = transaction.TransactionDate
        });

        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Transaction {TransactionId} updated successfully", request.Id);

        return mapper.Map<TransactionResponse>(transaction);
    }
}
