using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Investments.BatchDeleteInvestments;

public class BatchDeleteInvestmentsHandler(
    IInvestmentRepository investmentRepo,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator) : UserAwareHandler<BatchDeleteInvestmentsCommand, BatchDeleteInvestmentsResponse>(currentUserService)
{
    public override async Task<BatchDeleteInvestmentsResponse> Handle(
        BatchDeleteInvestmentsCommand request,
        CancellationToken cancellationToken)
    {
        var result = await investmentRepo.BatchDeleteAsync(
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
