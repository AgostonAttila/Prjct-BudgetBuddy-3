using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Services;

namespace BudgetBuddy.Module.Budgets.Features.UpdateBudget;

public class UpdateBudgetHandler(
    BudgetsDbContext context,
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

        var budget = await context.Budgets
            .FirstOrDefaultAsync(b => b.Id == request.Id && b.UserId == UserId, cancellationToken);

        if (budget == null)
            throw new NotFoundException(nameof(Budget), request.Id);

        budget.Name = request.Name;
        budget.Amount = request.Amount;

        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Budget {BudgetId} updated successfully", request.Id);

        return mapper.Map<BudgetResponse>(budget);
    }
}
