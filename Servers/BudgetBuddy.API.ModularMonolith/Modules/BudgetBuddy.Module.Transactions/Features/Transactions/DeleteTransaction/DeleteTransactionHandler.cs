using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Shared.Contracts.Events.Transactions;

namespace BudgetBuddy.Module.Transactions.Features.DeleteTransaction;

public class DeleteTransactionHandler(
    TransactionsDbContext _context,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator _cacheInvalidator,
    IDomainEventCollector _eventCollector,
    ILogger<DeleteTransactionHandler> _logger) : UserAwareHandler<DeleteTransactionCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteTransactionCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting transaction {TransactionId} for user {UserId}", request.Id, UserId);

        var transaction = await _context.Transactions
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.UserId == UserId, cancellationToken);

        if (transaction == null)
            throw new NotFoundException(nameof(Transaction), request.Id);

        _context.Transactions.Remove(transaction);

        _eventCollector.Collect(new TransactionDeletedEvent(
            EventId: Guid.NewGuid(),
            TransactionId: transaction.Id,
            UserId: UserId
        ));

        await _context.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        _logger.LogInformation("Transaction {TransactionId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
