using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Services;

namespace BudgetBuddy.Module.Budgets.Features.CreateBudget;

public class CreateBudgetHandler(
    BudgetsDbContext context,
    ICategoryQueryService categoryQueryService,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IUserCacheInvalidator cacheInvalidator,
    ILogger<CreateBudgetHandler> logger) : UserAwareHandler<CreateBudgetCommand, BudgetResponse>(currentUserService)
{
    public override async Task<BudgetResponse> Handle(
        CreateBudgetCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating budget for user {UserId}", UserId);

        var categories = await categoryQueryService.GetCategoriesByIdsAsync(
            [request.CategoryId], cancellationToken);

        if (!categories.TryGetValue(request.CategoryId, out var category))
            throw new NotFoundException("Category", request.CategoryId);

        if (await IsBudgetExist(request, UserId, cancellationToken))
            throw new ValidationException($"Budget already exists for category '{category.Name}' in {request.Year}-{request.Month:D2}");

        var budget = mapper.Map<Budget>(request);
        budget.UserId = UserId;

        await context.Budgets.AddAsync(budget, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Budget {BudgetId} created successfully", budget.Id);

        return mapper.Map<BudgetResponse>(budget);
    }

    private async Task<bool> IsBudgetExist(CreateBudgetCommand request, string userId, CancellationToken cancellationToken)
    {
        // Check if budget already exists for this category and period
        return await context.Budgets
            .AnyAsync(b =>
                    b.UserId == userId &&
                    b.CategoryId == request.CategoryId &&
                    b.Year == request.Year &&
                    b.Month == request.Month,
                cancellationToken);
    }
}
