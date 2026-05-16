using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using Microsoft.Extensions.Caching.Hybrid;

namespace BudgetBuddy.Service.ReferenceData.Features.Categories.GetCategories;

public class GetCategoriesHandler(
    ReferenceDataDbContext context,
    HybridCache hybridCache,
    ICurrentUserService currentUserService,
    ILogger<GetCategoriesHandler> logger) : UserAwareHandler<GetCategoriesQuery, List<CategoryDto>>(currentUserService)
{
    private static readonly Func<ReferenceDataDbContext, string, IAsyncEnumerable<CategoryDto>> GetByUserQuery =
        EF.CompileAsyncQuery((ReferenceDataDbContext ctx, string userId) =>
            ctx.Categories
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(c => new CategoryDto(
                    c.Id,
                    c.Name,
                    c.Icon,
                    c.Color,
                    c.Types.Count,  // Translates to COUNT subquery (not N+1)
                    0                 // Transaction count removed: cross-module navigation no longer available
                )));

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
                // SQL: SELECT *, (SELECT COUNT(*) FROM Types WHERE CategoryId = ...)
                var categoryDtos = new List<CategoryDto>();
                await foreach (var dto in GetByUserQuery(context, userId).WithCancellation(cancel))
                {
                    categoryDtos.Add(dto);
                }

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
