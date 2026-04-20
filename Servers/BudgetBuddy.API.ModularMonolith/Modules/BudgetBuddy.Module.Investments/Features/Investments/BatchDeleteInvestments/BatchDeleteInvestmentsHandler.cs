using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Services;

namespace BudgetBuddy.Module.Investments.Features.BatchDeleteInvestments;

public class BatchDeleteInvestmentsHandler(
    InvestmentsDbContext context,
    ICurrentUserService currentUserService,
    IBatchDeleteService batchDeleteService,
    IUserCacheInvalidator cacheInvalidator) : UserAwareHandler<BatchDeleteInvestmentsCommand, BatchDeleteInvestmentsResponse>(currentUserService)
{
    public override async Task<BatchDeleteInvestmentsResponse> Handle(
        BatchDeleteInvestmentsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await batchDeleteService.DeleteAsync(
            context.Investments,
            request.InvestmentIds,
            UserId,
            "Investment",
            cancellationToken);

        if (result.SuccessCount > 0)
            await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        return new BatchDeleteInvestmentsResponse(
            result.TotalRequested,
            result.SuccessCount,
            result.FailedCount,
            result.Errors);
    }
}
