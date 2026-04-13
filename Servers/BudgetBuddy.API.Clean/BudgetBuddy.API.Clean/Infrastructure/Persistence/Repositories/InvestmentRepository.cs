using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Infrastructure.Persistence.Repositories;

public class InvestmentRepository(AppDbContext context) : UserOwnedRepository<Investment>(context), IInvestmentRepository
{
    public async Task<(List<InvestmentPageItem> Items, int TotalCount)> GetPagedAsync(
        InvestmentFilter filter, CancellationToken ct = default)
    {
        var query = Context.Investments.Where(i => i.UserId == filter.UserId);

        if (filter.Type.HasValue)
            query = query.Where(i => i.Type == filter.Type.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchQuery = string.Join(" & ", filter.SearchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            query = query.Where(i =>
                EF.Functions.ToTsVector("english",
                    i.Symbol + " " + i.Name + " " + (i.Note ?? "")
                ).Matches(EF.Functions.ToTsQuery("english", searchQuery)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderBy(i => i.Symbol)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(i => new InvestmentPageItem(
                i.Id,
                i.Symbol,
                i.Name,
                i.Type,
                i.Quantity,
                i.PurchasePrice,
                i.CurrencyCode,
                i.PurchaseDate,
                i.Note,
                i.Account != null ? i.Account.Name : null))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<List<Investment>> GetForExportAsync(ExportInvestmentFilter filter, CancellationToken ct = default)
    {
        var query = Context.Investments
            .AsNoTracking()
            .Include(i => i.Account)
            .Where(i => i.UserId == filter.UserId);

        if (filter.Type.HasValue)
            query = query.Where(i => i.Type == filter.Type.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(i =>
                i.Symbol.ToLower().Contains(searchLower) ||
                i.Name.ToLower().Contains(searchLower) ||
                (i.Note != null && i.Note.ToLower().Contains(searchLower)));
        }

        return await query
            .OrderBy(i => i.Type)
            .ThenBy(i => i.Symbol)
            .ToListAsync(ct);
    }

    public async Task<BatchDeleteResult> BatchDeleteAsync(
        List<Guid> ids, string userId, string entityName, CancellationToken ct = default)
    {
        var existing = await Context.Investments
            .Where(i => ids.Contains(i.Id) && i.UserId == userId)
            .Select(i => i.Id)
            .ToListAsync(ct);

        var notFound = ids.Except(existing).ToList();
        var errors = notFound
            .Select(id => $"{entityName} {id} not found or does not belong to user")
            .ToList();

        var successCount = 0;
        if (existing.Count > 0)
        {
            successCount = await Context.Investments
                .Where(i => existing.Contains(i.Id) && i.UserId == userId)
                .ExecuteDeleteAsync(ct);
        }

        return new BatchDeleteResult(ids.Count, successCount, ids.Count - successCount, errors);
    }
}
