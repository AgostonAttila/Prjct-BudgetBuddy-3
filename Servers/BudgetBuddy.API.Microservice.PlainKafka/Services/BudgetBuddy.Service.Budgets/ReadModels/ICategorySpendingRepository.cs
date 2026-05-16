namespace BudgetBuddy.Service.Budgets.ReadModels;

public interface ICategorySpendingRepository
{
    Task<List<CategorySpendingAggregate>> GetByUserAndDateRangeAsync(
        string userId, int startYear, int startMonth, int endYear, int endMonth,
        CancellationToken ct);

    Task<List<CategorySpendingAggregate>> GetByUserCategoryAndDateRangeAsync(
        string userId, IEnumerable<Guid> categoryIds,
        int startYear, int startMonth, int endYear, int endMonth,
        CancellationToken ct);

    Task AddToExpenseAsync(
        string userId, Guid categoryId, int year, int month, string currencyCode,
        decimal delta, CancellationToken ct);
}
