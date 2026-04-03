using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using BudgetBuddy.API.VSA.Common.Shared.Services;

namespace BudgetBuddy.API.VSA.Features.Transactions.BatchDeleteTransactions;

public class BatchDeleteTransactionsHandler(
    AppDbContext context,
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
