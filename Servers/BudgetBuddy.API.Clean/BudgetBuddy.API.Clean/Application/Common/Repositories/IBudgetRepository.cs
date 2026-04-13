namespace BudgetBuddy.Application.Common.Repositories;

public record BudgetVsActualItem(Guid CategoryId, string CategoryName, decimal Amount);

public interface IBudgetRepository : IUserOwnedRepository<Budget>
{
    Task<bool> ExistsForMonthAsync(string userId, Guid categoryId, int year, int month, CancellationToken ct = default);
    Task<List<Budget>> GetFilteredAsync(string userId, int? year, int? month, Guid? categoryId, CancellationToken ct = default);
    Task<List<BudgetVsActualItem>> GetForVsActualAsync(string userId, int year, int month, CancellationToken ct = default);
}
