using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Budgets.DeleteBudget;

public class DeleteBudgetHandler(
    IBudgetRepository budgetRepo,
    IUnitOfWork uow,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<DeleteBudgetHandler> logger) : UserAwareHandler<DeleteBudgetCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteBudgetCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting budget {BudgetId} for user {UserId}", request.Id, UserId);

        var budget = await budgetRepo.GetByIdAsync(request.Id, UserId, cancellationToken);

        if (budget == null)
            throw new NotFoundException(nameof(Budget), request.Id);

        budgetRepo.Remove(budget);
        await uow.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Budget {BudgetId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
