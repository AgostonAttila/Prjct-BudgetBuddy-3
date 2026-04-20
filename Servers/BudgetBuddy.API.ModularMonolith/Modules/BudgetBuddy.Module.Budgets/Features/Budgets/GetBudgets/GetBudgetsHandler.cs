using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.Budgets.Features.GetBudgets;

public class GetBudgetsHandler(
    BudgetsDbContext context,
    ICurrentUserService currentUserService,
    ILogger<GetBudgetsHandler> logger) : UserAwareHandler<GetBudgetsQuery, GetBudgetsResponse>(currentUserService)
{
    public override async Task<GetBudgetsResponse> Handle(
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

        var totalCount = await query.CountAsync(cancellationToken);

        var budgets = await query
            .AsNoTracking()
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BudgetDto(
                b.Id,
                b.Name,
                b.CategoryId,
                string.Empty, // CategoryName: cross-module navigation removed — use CategoryId for lookups
                b.Amount,
                b.CurrencyCode,
                b.Year,
                b.Month
            ))
            .ToListAsync(cancellationToken);

        logger.LogInformation("Found {Count}/{Total} budgets for user {UserId}", budgets.Count, totalCount, UserId);

        return new GetBudgetsResponse(budgets, totalCount, request.PageNumber, request.PageSize);
    }
}
