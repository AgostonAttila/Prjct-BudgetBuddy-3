using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Shared.Contracts.Events.Transactions;

namespace BudgetBuddy.Module.Transactions.Features.UpdateTransaction;

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
            throw new NotFoundException(nameof(Transaction), request.Id);

        mapper.Map(request, transaction);

        eventCollector.Collect(new TransactionUpdatedEvent(
            EventId: Guid.NewGuid(),
            TransactionId: transaction.Id,
            UserId: UserId,
            AccountId: transaction.AccountId,
            CategoryId: transaction.CategoryId,
            Amount: transaction.Amount,
            CurrencyCode: transaction.CurrencyCode,
            TransactionType: (int)transaction.TransactionType,
            TransactionDate: transaction.TransactionDate
        ));

        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Transaction {TransactionId} updated successfully", request.Id);

        return mapper.Map<TransactionResponse>(transaction);
    }
}
