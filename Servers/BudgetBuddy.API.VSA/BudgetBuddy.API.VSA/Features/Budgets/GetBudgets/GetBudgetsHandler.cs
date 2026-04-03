using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;

namespace BudgetBuddy.API.VSA.Features.Budgets.GetBudgets;

public class GetBudgetsHandler(
    AppDbContext context,
    ICurrentUserService currentUserService,
    ILogger<GetBudgetsHandler> logger) : UserAwareHandler<GetBudgetsQuery, List<BudgetDto>>(currentUserService)
{


    public override async Task<List<BudgetDto>> Handle(
        GetBudgetsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching budgets for user {UserId}", UserId);

        var query = context.Budgets
            .Where(b => b.UserId == UserId);

        if (request.Year.HasValue)
            query = query.Where(b => b.Year == request.Year.Value);

        if (request.Month.HasValue)
            query = query.Where(b => b.Month == request.Month.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(b => b.CategoryId == request.CategoryId.Value);


        var budgets = await query
            //.Include(b => b.Category) because of projection        
            .AsNoTracking()
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.Category.Name)
            .Select(b => new BudgetDto(
                b.Id,
                b.Name,
                b.CategoryId,
                b.Category.Name,
                b.Amount,
                b.CurrencyCode,
                b.Year,
                b.Month
            ))
            .ToListAsync(cancellationToken);

        logger.LogInformation("Found {Count} budgets for user {UserId}", budgets.Count, UserId);

        return budgets;
    }
}
