using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using BudgetBuddy.API.VSA.Common.Shared.Services;

namespace BudgetBuddy.API.VSA.Features.Budgets.CreateBudget;

public class CreateBudgetHandler(
    AppDbContext context,
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

        var category = await GetCategory(request, UserId, cancellationToken);

        if (category == null)
            throw new NotFoundException(nameof(Category), request.CategoryId);

        if (await IsBudgetExist(request, UserId, cancellationToken))
            throw new ValidationException($"Budget already exists for category '{category.Name}' in {request.Year}-{request.Month:D2}");

        var budget = mapper.Map<Budget>(request);
        budget.UserId = UserId;
        budget.Category = category;

        await context.Budgets.AddAsync(budget, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await cacheInvalidator.InvalidateAsync(UserId, cancellationToken);

        logger.LogInformation("Budget {BudgetId} created successfully", budget.Id);

        return mapper.Map<BudgetResponse>(budget);
    }

    private async Task<Category?> GetCategory(CreateBudgetCommand request, string userId, CancellationToken cancellationToken)
    {
        // Verify category exists and belongs to user
        return await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.CategoryId && c.UserId == userId, cancellationToken);
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
