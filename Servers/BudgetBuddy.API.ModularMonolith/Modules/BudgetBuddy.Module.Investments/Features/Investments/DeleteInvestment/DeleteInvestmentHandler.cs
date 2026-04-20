using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Services;

namespace BudgetBuddy.Module.Investments.Features.DeleteInvestment;

public class DeleteInvestmentHandler(
    InvestmentsDbContext context,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<DeleteInvestmentHandler> logger) : UserAwareHandler<DeleteInvestmentCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteInvestmentCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting investment {InvestmentId} for user {UserId}", request.Id, UserId);

        var investment = await context.Investments
            .FirstOrDefaultAsync(i => i.Id == request.Id && i.UserId == UserId, cancellationToken);

        if (investment == null)
            throw new NotFoundException(nameof(Investment), request.Id);

        context.Investments.Remove(investment);
        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Investment {InvestmentId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
