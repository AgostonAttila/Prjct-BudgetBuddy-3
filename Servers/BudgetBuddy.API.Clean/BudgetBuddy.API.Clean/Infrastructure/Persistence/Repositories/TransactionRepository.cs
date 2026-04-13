using System.Text.RegularExpressions;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Infrastructure.Persistence.Repositories;

public partial class TransactionRepository(AppDbContext context) : UserOwnedRepository<Transaction>(context), ITransactionRepository
{
    public async Task<(List<TransactionPageItem> Items, int TotalCount)> GetPagedAsync(
        TransactionFilter filter, CancellationToken ct = default)
    {
        var query = Context.Transactions.Where(t => t.UserId == filter.UserId);

        if (filter.AccountId.HasValue)
            query = query.Where(t => t.AccountId == filter.AccountId.Value);
        if (filter.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
        if (filter.StartDate.HasValue)
            query = query.Where(t => t.TransactionDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
            query = query.Where(t => t.TransactionDate <= filter.EndDate.Value);
        if (filter.Type.HasValue)
            query = query.Where(t => t.TransactionType == filter.Type.Value);
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            query = ApplySearch(query, filter.SearchTerm);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TransactionPageItem(
                t.Id,
                t.Account.Name,
                t.Category != null ? t.Category.Name : null,
                t.Amount,
                t.CurrencyCode,
                t.TransactionType,
                t.PaymentType,
                t.TransactionDate,
                t.IsTransfer))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<List<TransactionBalanceAggregate>> GetBalanceAggregatesByAccountAsync(
        IList<Guid> accountIds, LocalDate? upToDate, CancellationToken ct = default)
    {
        var query = Context.Transactions
            .AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId));

        if (upToDate.HasValue)
            query = query.Where(t => t.TransactionDate <= upToDate.Value);

        return await query
            .GroupBy(t => new { t.AccountId, t.TransactionType })
            .Select(g => new TransactionBalanceAggregate(
                g.Key.AccountId,
                g.Key.TransactionType,
                g.Sum(t => t.Amount)))
            .ToListAsync(ct);
    }

    public async Task<List<Transaction>> GetByIdsAsync(
        IEnumerable<Guid> ids, string userId, CancellationToken ct = default)
    {
        return await Context.Transactions
            .Where(t => ids.Contains(t.Id) && t.UserId == userId)
            .ToListAsync(ct);
    }

    public async Task<List<Transaction>> GetForExportAsync(
        ExportTransactionFilter filter, CancellationToken ct = default)
    {
        var query = Context.Transactions
            .AsNoTracking()
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => t.UserId == filter.UserId);

        if (filter.AccountId.HasValue)
            query = query.Where(t => t.AccountId == filter.AccountId.Value);
        if (filter.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
        if (filter.TransactionType.HasValue)
            query = query.Where(t => t.TransactionType == filter.TransactionType.Value);
        if (filter.StartDate.HasValue)
            query = query.Where(t => t.TransactionDate >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
            query = query.Where(t => t.TransactionDate <= filter.EndDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var searchLower = filter.Search.ToLower();
            query = query.Where(t =>
                (t.Note != null && t.Note.ToLower().Contains(searchLower)) ||
                (t.Payee != null && t.Payee.ToLower().Contains(searchLower)) ||
                (t.Labels != null && t.Labels.ToLower().Contains(searchLower)));
        }

        return await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Dictionary<Guid, decimal>> GetSpendingByCategoryAsync(
        string userId, IList<Guid> categoryIds, LocalDate startDate, LocalDate endDate, CancellationToken ct = default)
    {
        return await Context.Transactions
            .AsNoTracking()
            .Where(t =>
                t.UserId == userId &&
                categoryIds.Contains(t.CategoryId!.Value) &&
                t.TransactionType == TransactionType.Expense &&
                t.TransactionDate >= startDate &&
                t.TransactionDate <= endDate)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key!.Value, Amount = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Amount, ct);
    }

    public async Task<(decimal TotalIncome, decimal TotalExpense, int TransactionCount)> GetAccountBalanceSummaryAsync(
        Guid accountId, CancellationToken ct = default)
    {
        var groups = await Context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .GroupBy(t => t.TransactionType)
            .Select(g => new { Type = g.Key, Total = g.Sum(t => t.Amount), Count = g.Count() })
            .ToListAsync(ct);

        var income = groups.FirstOrDefault(x => x.Type == TransactionType.Income)?.Total ?? 0;
        var expense = groups.FirstOrDefault(x => x.Type == TransactionType.Expense)?.Total ?? 0;
        var count = groups.Sum(x => x.Count);

        return (income, expense, count);
    }

    public async Task<BatchDeleteResult> BatchDeleteAsync(
        List<Guid> ids, string userId, string entityName, CancellationToken ct = default)
    {
        var existing = await Context.Transactions
            .Where(t => ids.Contains(t.Id) && t.UserId == userId)
            .Select(t => t.Id)
            .ToListAsync(ct);

        var notFound = ids.Except(existing).ToList();
        var errors = notFound
            .Select(id => $"{entityName} {id} not found or does not belong to user")
            .ToList();

        var successCount = 0;
        if (existing.Count > 0)
        {
            successCount = await Context.Transactions
                .Where(t => existing.Contains(t.Id) && t.UserId == userId)
                .ExecuteDeleteAsync(ct);
        }

        return new BatchDeleteResult(ids.Count, successCount, ids.Count - successCount, errors);
    }

    private static IQueryable<Transaction> ApplySearch(IQueryable<Transaction> query, string searchTerm)
    {
        var sanitized = NonWordRegex().Replace(searchTerm, "");
        var ftsQuery = string.Join(" & ", sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrWhiteSpace(ftsQuery))
            return query;

        return query.Where(t =>
            EF.Functions.ToTsVector("english",
                (t.Note ?? "") + " " + (t.Payee ?? "") + " " + (t.Labels ?? "")
            ).Matches(EF.Functions.ToTsQuery("english", ftsQuery))
        );
    }

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex NonWordRegex();
}
