using Microsoft.EntityFrameworkCore;

namespace BudgetBuddy.Service.Budgets.ReadModels;

public class CategorySpendingRepository(BudgetsDbContext context) : ICategorySpendingRepository
{
    public Task<List<CategorySpendingAggregate>> GetByUserAndDateRangeAsync(
        string userId, int startYear, int startMonth, int endYear, int endMonth,
        CancellationToken ct)
        => context.CategorySpendingAggregates
            .Where(a => a.UserId == userId
                && (a.Year > startYear || (a.Year == startYear && a.Month >= startMonth))
                && (a.Year < endYear   || (a.Year == endYear   && a.Month <= endMonth)))
            .ToListAsync(ct);

    public Task<List<CategorySpendingAggregate>> GetByUserCategoryAndDateRangeAsync(
        string userId, IEnumerable<Guid> categoryIds,
        int startYear, int startMonth, int endYear, int endMonth,
        CancellationToken ct)
    {
        var ids = categoryIds.ToList();
        return context.CategorySpendingAggregates
            .Where(a => a.UserId == userId
                && ids.Contains(a.CategoryId)
                && (a.Year > startYear || (a.Year == startYear && a.Month >= startMonth))
                && (a.Year < endYear   || (a.Year == endYear   && a.Month <= endMonth)))
            .ToListAsync(ct);
    }

    public async Task AddToExpenseAsync(
        string userId, Guid categoryId, int year, int month, string currencyCode,
        decimal delta, CancellationToken ct)
    {
        var aggregate = await context.CategorySpendingAggregates
            .FirstOrDefaultAsync(a =>
                a.UserId == userId &&
                a.CategoryId == categoryId &&
                a.Year == year &&
                a.Month == month &&
                a.CurrencyCode == currencyCode, ct);

        if (aggregate is null)
        {
            aggregate = new CategorySpendingAggregate
            {
                Id           = Guid.NewGuid(),
                UserId       = userId,
                CategoryId   = categoryId,
                Year         = year,
                Month        = month,
                CurrencyCode = currencyCode,
                TotalExpense = Math.Max(0, delta)
            };
            context.CategorySpendingAggregates.Add(aggregate);
        }
        else
        {
            aggregate.TotalExpense = Math.Max(0, aggregate.TotalExpense + delta);
        }

        await context.SaveChangesAsync(ct);
    }
}
