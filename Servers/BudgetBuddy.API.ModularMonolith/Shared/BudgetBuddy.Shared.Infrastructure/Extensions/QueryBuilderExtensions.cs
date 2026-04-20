namespace BudgetBuddy.Shared.Infrastructure.Extensions;

/// <summary>
/// Generic query extension helpers shared across modules.
/// Module-specific query builders live in their own module (e.g., Module.Transactions, Module.Investments).
/// </summary>
public static class QueryBuilderExtensions
{
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, System.Linq.Expressions.Expression<Func<T, bool>> predicate)
        => condition ? query.Where(predicate) : query;
}
