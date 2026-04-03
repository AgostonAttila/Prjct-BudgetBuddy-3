namespace BudgetBuddy.API.VSA.Features.Transactions.Services;

public interface ITransactionSearchService
{
    /// <summary>
    /// Applies a search term to the query using PostgreSQL full-text search,
    /// falling back to ILIKE if the FTS query is invalid.
    /// </summary>
    IQueryable<Transaction> ApplySearch(IQueryable<Transaction> query, string searchTerm);
}
