namespace BudgetBuddy.Application.Common.Repositories;

public interface IUserOwnedRepository<T> : IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, string userId, CancellationToken ct = default);
    Task<List<T>> GetByUserAsync(string userId, CancellationToken ct = default);
}
