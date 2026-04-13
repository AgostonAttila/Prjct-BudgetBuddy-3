using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Investments.DeleteInvestment;

public class DeleteInvestmentHandler(
    IInvestmentRepository investmentRepo,
    IUnitOfWork uow,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<DeleteInvestmentHandler> logger) : UserAwareHandler<DeleteInvestmentCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteInvestmentCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting investment {InvestmentId} for user {UserId}", request.Id, UserId);

        var investment = await investmentRepo.GetByIdAsync(request.Id, UserId, cancellationToken);

        if (investment == null)
            throw new NotFoundException(nameof(Investment), request.Id);

        investmentRepo.Remove(investment);
        await uow.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Investment {InvestmentId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
