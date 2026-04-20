using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Services;

namespace BudgetBuddy.Module.Transactions.Features.BatchDeleteTransactions;

public class BatchDeleteTransactionsHandler(
    TransactionsDbContext context,
    ICurrentUserService currentUserService,
    IBatchDeleteService batchDeleteService,
    IUserCacheInvalidator cacheInvalidator) : UserAwareHandler<BatchDeleteTransactionsCommand, BatchDeleteTransactionsResponse>(currentUserService)
{
    public override async Task<BatchDeleteTransactionsResponse> Handle(
        BatchDeleteTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await batchDeleteService.DeleteAsync(
            context.Transactions,
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
