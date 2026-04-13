using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Transactions.UpdateTransaction;

public class UpdateTransactionHandler(
    ITransactionRepository transactionRepo,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<UpdateTransactionHandler> logger) : UserAwareHandler<UpdateTransactionCommand, TransactionResponse>(currentUserService)
{
    public override async Task<TransactionResponse> Handle(
        UpdateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating transaction {TransactionId} for user {UserId}", request.Id, UserId);

        var transaction = await transactionRepo.GetByIdAsync(request.Id, UserId, cancellationToken);

        if (transaction == null)
            throw new NotFoundException(nameof(Transaction), request.Id);

        mapper.Map(request, transaction);

        await uow.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Transaction {TransactionId} updated successfully", request.Id);

        return mapper.Map<TransactionResponse>(transaction);
    }
}
