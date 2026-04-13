using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Transactions.BatchDeleteTransactions;

public class BatchDeleteTransactionsHandler(
    ITransactionRepository transactionRepo,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator) : UserAwareHandler<BatchDeleteTransactionsCommand, BatchDeleteTransactionsResponse>(currentUserService)
{
    public override async Task<BatchDeleteTransactionsResponse> Handle(
        BatchDeleteTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await transactionRepo.BatchDeleteAsync(
            request.TransactionIds,
            UserId,
            "Transaction",
            cancellationToken);

        if (result.SuccessCount > 0)
            await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        return new BatchDeleteTransactionsResponse(
            result.TotalRequested,
            result.SuccessCount,
            result.FailedCount,
            result.Errors);
    }
}
