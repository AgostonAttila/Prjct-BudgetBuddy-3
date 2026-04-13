namespace BudgetBuddy.Application.Common.Repositories;

public interface ICurrencyRepository : IRepository<Currency>
{
    Task<List<Currency>> GetAllOrderedAsync(CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default);
}
