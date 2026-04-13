using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Transactions.DeleteTransaction;

public class DeleteTransactionHandler(
    ITransactionRepository transactionRepo,
    IUnitOfWork uow,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<DeleteTransactionHandler> logger) : UserAwareHandler<DeleteTransactionCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteTransactionCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting transaction {TransactionId} for user {UserId}", request.Id, UserId);

        var transaction = await transactionRepo.GetByIdAsync(request.Id, UserId, cancellationToken);

        if (transaction == null)
            throw new NotFoundException(nameof(Transaction), request.Id);

        transactionRepo.Remove(transaction);
        await uow.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Transaction {TransactionId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
