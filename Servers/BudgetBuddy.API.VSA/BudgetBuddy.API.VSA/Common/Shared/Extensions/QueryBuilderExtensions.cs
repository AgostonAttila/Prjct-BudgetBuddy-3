using BudgetBuddy.API.VSA.Common.Domain.Enums;
using NodaTime;

namespace BudgetBuddy.API.VSA.Common.Shared.Extensions;

/// <summary>
/// Extension methods for building filtered queries
/// </summary>
public static class QueryBuilderExtensions
{
    /// <summary>
    /// Builds a filtered transaction query with common filters
    /// </summary>
    /// <param name="transactions">Transaction DbSet or queryable</param>
    /// <param name="userId">User ID to filter by (required)</param>
    /// <param name="startDate">Optional start date (inclusive)</param>
    /// <param name="endDate">Optional end date (inclusive)</param>
    /// <param name="accountId">Optional account ID filter</param>
    /// <returns>Filtered IQueryable of transactions</returns>
    public static IQueryable<Common.Domain.Entities.Transaction> FilterByUser(
        this IQueryable<Common.Domain.Entities.Transaction> transactions,
        string userId,
        LocalDate? startDate = null,
        LocalDate? endDate = null,
        Guid? accountId = null)
    {
        var query = transactions.Where(t => t.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(t => t.TransactionDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.TransactionDate <= endDate.Value);

        if (accountId.HasValue)
            query = query.Where(t => t.AccountId == accountId.Value);

        return query;
    }

    /// <summary>
    /// Builds a filtered investment query with common filters
    /// </summary>
    /// <param name="investments">Investment DbSet or queryable</param>
    /// <param name="userId">User ID to filter by (required)</param>
    /// <param name="startDate">Optional purchase start date (inclusive)</param>
    /// <param name="endDate">Optional purchase end date (inclusive)</param>
    /// <param name="type">Optional investment type filter</param>
    /// <returns>Filtered IQueryable of investments</returns>
    public static IQueryable<Common.Domain.Entities.Investment> FilterByUser(
        this IQueryable<Common.Domain.Entities.Investment> investments,
        string userId,
        LocalDate? startDate = null,
        LocalDate? endDate = null,
        InvestmentType? type = null)
    {
        var query = investments.Where(i => i.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(i => i.PurchaseDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(i => i.PurchaseDate <= endDate.Value);

        if (type.HasValue)
            query = query.Where(i => i.Type == type.Value);

        return query;
    }
}
