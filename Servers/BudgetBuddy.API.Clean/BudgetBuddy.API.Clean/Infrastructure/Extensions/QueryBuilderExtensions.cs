namespace BudgetBuddy.Infrastructure.Extensions;

public static class QueryBuilderExtensions
{
    public static IQueryable<Transaction> FilterByUser(
        this IQueryable<Transaction> transactions,
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

    public static IQueryable<Investment> FilterByUser(
        this IQueryable<Investment> investments,
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
