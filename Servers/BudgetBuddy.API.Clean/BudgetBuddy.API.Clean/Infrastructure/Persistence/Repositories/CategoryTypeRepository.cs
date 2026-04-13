using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Infrastructure.Persistence.Repositories;

public class CategoryTypeRepository(AppDbContext context) : Repository<CategoryType>(context), ICategoryTypeRepository
{
    public async Task<(List<CategoryType> Items, int TotalCount)> GetPagedAsync(
        string userId, Guid? categoryId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = Context.CategoryTypes
            .AsNoTracking()
            .Include(ct => ct.Category)
            .Where(ct => ct.Category.UserId == userId);

        if (categoryId.HasValue)
            query = query.Where(ct => ct.CategoryId == categoryId.Value);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(ct => ct.Category.Name)
            .ThenBy(ct => ct.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<CategoryType?> GetWithCategoryAsync(Guid id, CancellationToken ct = default)
    {
        return await Context.CategoryTypes
            .Include(ct => ct.Category)
            .FirstOrDefaultAsync(ct => ct.Id == id, ct);
    }
}
