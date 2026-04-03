using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using BudgetBuddy.API.VSA.Common.Shared.Services;

namespace BudgetBuddy.API.VSA.Features.Budgets.DeleteBudget;

public class DeleteBudgetHandler(
    AppDbContext context,
    ICurrentUserService currentUserService,
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
            throw new NotFoundException(nameof(Budget), request.Id);

        context.Budgets.Remove(budget);
        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Budget {BudgetId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
