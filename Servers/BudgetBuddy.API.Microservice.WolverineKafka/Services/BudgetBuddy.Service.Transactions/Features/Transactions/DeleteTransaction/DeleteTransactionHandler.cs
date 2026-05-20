using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Transactions.Features.DeleteTransaction;

public class DeleteTransactionHandler(
    TransactionsDbContext _context,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator _cacheInvalidator,
    IDbContextOutbox _outbox,
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
        {
            throw new NotFoundException(nameof(Transaction), request.Id);
        }

        _context.Transactions.Remove(transaction);

        _outbox.Enroll(_context);
        await _outbox.SendAsync(new TransactionDeletedEvent
        {
            TransactionId = transaction.Id,
            UserId = UserId,
            AccountId = transaction.AccountId,
            Amount = transaction.Amount,
            CurrencyCode = transaction.CurrencyCode,
            TransactionType = transaction.TransactionType,
            CategoryId = transaction.CategoryId,
            TransactionDate = transaction.TransactionDate
        });

        await _outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        _logger.LogInformation("Transaction {TransactionId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
