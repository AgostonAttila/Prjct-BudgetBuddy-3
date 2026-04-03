using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using Microsoft.Extensions.Caching.Hybrid;

namespace BudgetBuddy.API.VSA.Features.Categories.GetCategories;

public class GetCategoriesHandler(
    AppDbContext context,
    HybridCache hybridCache,
    ICurrentUserService currentUserService,
    ILogger<GetCategoriesHandler> logger) : UserAwareHandler<GetCategoriesQuery, List<CategoryDto>>(currentUserService)
{


    public override async Task<List<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching categories for user {UserId}", UserId);

        var categories = await GetCategories(UserId, cancellationToken);

        logger.LogInformation("Found {Count} categories for user {UserId}", categories.Count, UserId);

        return categories;
    }

    private async Task<List<CategoryDto>> GetCategories(string userId, CancellationToken cancellationToken)
    {
        // Hybrid cache for reference data (categories change infrequently)
        var cacheKey = $"categories:list:{userId}";

        return await hybridCache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                // PERFORMANCE NOTE: .Count() in Select generates SQL subqueries (efficient, single DB round-trip)
                // Alternative: GroupJoin with Count would require .Include() and in-memory aggregation (slower)
                // SQL: SELECT *, (SELECT COUNT(*) FROM Types ...), (SELECT COUNT(*) FROM Transactions ...)
                var categoryDtos = await context.Categories
                    .AsNoTracking()
                    .Where(c => c.UserId == userId)
                    .Select(c => new CategoryDto(
                        c.Id,
                        c.Name,
                        c.Icon,
                        c.Color,
                        c.Types.Count(),        // Translates to COUNT subquery (not N+1)
                        c.Transactions.Count()  // Translates to COUNT subquery (not N+1)
                    ))
                    .ToListAsync(cancel);

                return categoryDtos;
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(30),      // L2 cache: 30 min (reference data)
                LocalCacheExpiration = TimeSpan.FromMinutes(5) // L1 cache: 5 min
            },
            cancellationToken: cancellationToken
        );
    }
}
