using BudgetBuddy.Shared.Contracts.ReferenceData;

namespace BudgetBuddy.Module.ReferenceData.Features.Categories.Services;

public class CategoryQueryService(ReferenceDataDbContext context) : ICategoryQueryService
{
    public async Task<Dictionary<Guid, CategoryInfo>> GetCategoriesByIdsAsync(
        IEnumerable<Guid> categoryIds,
        CancellationToken cancellationToken = default)
    {
        var ids = categoryIds.ToList();
        var categories = await context.Categories
            .AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new CategoryInfo(c.Id, c.Name, c.Icon))
            .ToListAsync(cancellationToken);

        return categories.ToDictionary(c => c.Id);
    }

    public async Task<Dictionary<string, Guid>> GetUserCategoryNameMapAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await context.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .ToDictionaryAsync(c => c.Name, c => c.Id, cancellationToken);
    }
}
