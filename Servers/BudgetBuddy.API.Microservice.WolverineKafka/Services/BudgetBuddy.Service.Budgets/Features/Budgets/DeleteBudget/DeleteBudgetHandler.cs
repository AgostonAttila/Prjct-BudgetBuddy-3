using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Shared.Messages.Events.Budgets;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.Budgets.Features.DeleteBudget;

public class DeleteBudgetHandler(
    BudgetsDbContext context,
    ICurrentUserService currentUserService,
    IDbContextOutbox outbox,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<DeleteBudgetHandler> logger) : UserAwareHandler<DeleteBudgetCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteBudgetCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting budget {BudgetId} for user {UserId}", request.Id, UserId);

        var budget = await context.Budgets
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.UserId == UserId, cancellationToken);

        if (budget == null)
        {
            throw new NotFoundException(nameof(Budget), request.Id);
        }

        context.Budgets.Remove(budget);

        outbox.Enroll(context);
        await outbox.SendAsync(new BudgetDeletedEvent
        {
            BudgetId = budget.Id,
            UserId   = UserId,
        });
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Budget {BudgetId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
