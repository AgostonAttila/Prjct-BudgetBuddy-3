using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Categories.GetCategories;

public class GetCategoriesHandler(
    ICategoryRepository categoryRepo,
    IAppCache appCache,
    ICurrentUserService currentUserService,
    ILogger<GetCategoriesHandler> logger) : UserAwareHandler<GetCategoriesQuery, List<CategoryDto>>(currentUserService)
{
    private static readonly AppCacheOptions CacheOptions = new(
        Ttl: TimeSpan.FromMinutes(30),
        LocalTtl: TimeSpan.FromMinutes(5));

    public override async Task<List<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching categories for user {UserId}", UserId);

        var categories = await appCache.GetOrCreateAsync(
            $"categories:list:{UserId}",
            async cancel =>
            {
                // PERFORMANCE NOTE: GetSummariesAsync uses COUNT subqueries (efficient, single DB round-trip)
                var summaries = await categoryRepo.GetSummariesAsync(UserId, cancel);
                return summaries
                    .Select(s => new CategoryDto(s.Id, s.Name, s.Icon, s.Color, s.TypeCount, s.TransactionCount))
                    .ToList();
            },
            CacheOptions,
            ct: cancellationToken);

        logger.LogInformation("Found {Count} categories for user {UserId}", categories.Count, UserId);

        return categories;
    }
}
