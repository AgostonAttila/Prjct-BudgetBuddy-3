using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Budgets.UpdateBudget;

public class UpdateBudgetHandler(
    IBudgetRepository budgetRepo,
    IUnitOfWork uow,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<UpdateBudgetHandler> logger,
    IMapper mapper) : UserAwareHandler<UpdateBudgetCommand, BudgetResponse>(currentUserService)
{
    public override async Task<BudgetResponse> Handle(
        UpdateBudgetCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating budget {BudgetId} for user {UserId}", request.Id, UserId);

        var budget = await budgetRepo.GetByIdAsync(request.Id, UserId, cancellationToken);

        if (budget == null)
            throw new NotFoundException(nameof(Budget), request.Id);

        budget.Name = request.Name;
        budget.Amount = request.Amount;

        await uow.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Budget {BudgetId} updated successfully", request.Id);

        return mapper.Map<BudgetResponse>(budget);
    }
}
