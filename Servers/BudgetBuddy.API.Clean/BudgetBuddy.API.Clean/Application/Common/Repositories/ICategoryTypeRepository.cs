namespace BudgetBuddy.Application.Common.Repositories;

public interface ICategoryTypeRepository : IRepository<CategoryType>
{
    Task<(List<CategoryType> Items, int TotalCount)> GetPagedAsync(
        string userId, Guid? categoryId, int page, int pageSize, CancellationToken ct = default);
    Task<CategoryType?> GetWithCategoryAsync(Guid id, CancellationToken ct = default);
}
