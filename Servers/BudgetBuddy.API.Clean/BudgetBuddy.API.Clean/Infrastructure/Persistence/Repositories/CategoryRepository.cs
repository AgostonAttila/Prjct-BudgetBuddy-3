using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Infrastructure.Persistence.Repositories;

public class CategoryRepository(AppDbContext context) : UserOwnedRepository<Category>(context), ICategoryRepository
{
    public async Task<List<CategoryListSummary>> GetSummariesAsync(string userId, CancellationToken ct = default)
    {
        return await Context.Categories
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => new CategoryListSummary(
                c.Id,
                c.Name,
                c.Icon,
                c.Color,
                c.Types.Count(),
                c.Transactions.Count()))
            .ToListAsync(ct);
    }
}
