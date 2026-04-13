namespace BudgetBuddy.Application.Common.Repositories;

public record CategoryListSummary(
    Guid Id,
    string Name,
    string? Icon,
    string? Color,
    int TypeCount,
    int TransactionCount);

public interface ICategoryRepository : IUserOwnedRepository<Category>
{
    Task<List<CategoryListSummary>> GetSummariesAsync(string userId, CancellationToken ct = default);
}
