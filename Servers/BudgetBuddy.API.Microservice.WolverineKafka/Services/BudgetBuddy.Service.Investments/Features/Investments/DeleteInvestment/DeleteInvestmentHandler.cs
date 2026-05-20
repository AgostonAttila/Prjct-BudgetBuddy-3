using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Shared.Messages.Events.Investments;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Investments.Features.DeleteInvestment;

public class DeleteInvestmentHandler(
    InvestmentsDbContext context,
    ICurrentUserService currentUserService,
    IDbContextOutbox outbox,
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
        {
            throw new NotFoundException(nameof(Investment), request.Id);
        }

        context.Investments.Remove(investment);

        outbox.Enroll(context);
        await outbox.SendAsync(new InvestmentDeletedEvent
        {
            InvestmentId = investment.Id,
            UserId       = UserId,
        });
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Investment {InvestmentId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
