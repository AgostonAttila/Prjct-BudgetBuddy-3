using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using BudgetBuddy.API.VSA.Common.Shared.Services;

namespace BudgetBuddy.API.VSA.Features.Investments.BatchDeleteInvestments;

public class BatchDeleteInvestmentsHandler(
    AppDbContext context,
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
