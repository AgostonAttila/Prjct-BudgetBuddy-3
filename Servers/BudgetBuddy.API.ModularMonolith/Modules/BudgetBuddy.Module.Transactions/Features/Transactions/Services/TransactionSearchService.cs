using System.Text.RegularExpressions;
using Npgsql;

namespace BudgetBuddy.Module.Transactions.Features.Transactions.Services;

public partial class TransactionSearchService(ILogger<TransactionSearchService> logger) : ITransactionSearchService
{
    public IQueryable<Transaction> ApplySearch(IQueryable<Transaction> query, string searchTerm)
    {
        var sanitized = NonWordRegex().Replace(searchTerm, "");
        var ftsQuery = string.Join(" & ", sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (string.IsNullOrWhiteSpace(ftsQuery))
            return query;

        try
        {
            return query.Where(t =>
                EF.Functions.ToTsVector("english",
                    (t.Note ?? "") + " " + (t.Payee ?? "") + " " + (t.Labels ?? "")
                ).Matches(EF.Functions.ToTsQuery("english", ftsQuery))
            );
        }
        catch (PostgresException ex)
        {
            logger.LogWarning(ex, "Invalid FTS query: {SearchTerm}, falling back to ILIKE", searchTerm);

            var likePattern = $"%{sanitized}%";
            return query.Where(t =>
                EF.Functions.ILike(t.Note ?? "", likePattern) ||
                EF.Functions.ILike(t.Payee ?? "", likePattern) ||
                EF.Functions.ILike(t.Labels ?? "", likePattern)
            );
        }
    }

    [GeneratedRegex(@"[^\w\s]")]
    private static partial Regex NonWordRegex();
}
