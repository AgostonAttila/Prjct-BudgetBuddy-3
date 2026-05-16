using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;
using BudgetBuddy.Shared.Infrastructure.Services;
using BudgetBuddy.Shared.Messages.Events.Budgets;

namespace BudgetBuddy.Service.Budgets.Features.UpdateBudget;

public class UpdateBudgetHandler(
    BudgetsDbContext context,
    ICurrentUserService currentUserService,
    IDomainEventCollector collector,
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
        {
            throw new NotFoundException(nameof(Budget), request.Id);
        }

        budget.Name   = request.Name;
        budget.Amount = request.Amount;

        collector.Collect(new BudgetUpdatedEvent
        {
            BudgetId     = budget.Id,
            UserId       = UserId,
            CategoryId   = budget.CategoryId,
            Amount       = budget.Amount,
            CurrencyCode = budget.CurrencyCode,
            Year         = budget.Year,
            Month        = budget.Month,
        });

        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Budget {BudgetId} updated successfully", request.Id);

        return mapper.Map<BudgetResponse>(budget);
    }
}
