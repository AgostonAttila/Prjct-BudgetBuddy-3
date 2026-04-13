using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Infrastructure.Persistence.Repositories;

public class CurrencyRepository(AppDbContext context) : Repository<Currency>(context), ICurrencyRepository
{
    public async Task<List<Currency>> GetAllOrderedAsync(CancellationToken ct = default)
    {
        return await Context.Currencies
            .AsNoTracking()
            .OrderBy(c => c.Code)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken ct = default)
    {
        var query = Context.Currencies.Where(c => c.Code == code.ToUpperInvariant());
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync(ct);
    }
}
