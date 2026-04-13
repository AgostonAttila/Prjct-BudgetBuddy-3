using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Infrastructure.Persistence.Repositories;

public class BudgetRepository(AppDbContext context) : UserOwnedRepository<Budget>(context), IBudgetRepository
{
    public async Task<bool> ExistsForMonthAsync(
        string userId, Guid categoryId, int year, int month, CancellationToken ct = default)
    {
        return await Context.Budgets
            .AnyAsync(b =>
                b.UserId == userId &&
                b.CategoryId == categoryId &&
                b.Year == year &&
                b.Month == month, ct);
    }

    public async Task<List<Budget>> GetFilteredAsync(
        string userId, int? year, int? month, Guid? categoryId, CancellationToken ct = default)
    {
        var query = Context.Budgets
            .AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.UserId == userId);

        if (year.HasValue)
            query = query.Where(b => b.Year == year.Value);
        if (month.HasValue)
            query = query.Where(b => b.Month == month.Value);
        if (categoryId.HasValue)
            query = query.Where(b => b.CategoryId == categoryId.Value);

        return await query
            .OrderByDescending(b => b.Year)
            .ThenByDescending(b => b.Month)
            .ThenBy(b => b.Category.Name)
            .ToListAsync(ct);
    }

    public async Task<List<BudgetVsActualItem>> GetForVsActualAsync(
        string userId, int year, int month, CancellationToken ct = default)
    {
        return await Context.Budgets
            .AsNoTracking()
            .Where(b => b.UserId == userId && b.Year == year && b.Month == month)
            .Select(b => new BudgetVsActualItem(b.CategoryId, b.Category.Name, b.Amount))
            .ToListAsync(ct);
    }
}
