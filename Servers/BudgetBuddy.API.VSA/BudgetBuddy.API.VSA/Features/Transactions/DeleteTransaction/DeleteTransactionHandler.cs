using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using BudgetBuddy.API.VSA.Common.Shared.Services;

namespace BudgetBuddy.API.VSA.Features.Transactions.DeleteTransaction;

public class DeleteTransactionHandler(
    AppDbContext _context,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator _cacheInvalidator,
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
        await _context.SaveChangesAsync(cancellationToken);
        await _cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        _logger.LogInformation("Transaction {TransactionId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
