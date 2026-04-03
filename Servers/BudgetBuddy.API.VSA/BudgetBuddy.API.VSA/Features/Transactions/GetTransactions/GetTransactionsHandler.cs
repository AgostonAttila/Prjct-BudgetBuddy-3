using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;
using BudgetBuddy.API.VSA.Features.Transactions.Services;

namespace BudgetBuddy.API.VSA.Features.Transactions.GetTransactions;

public class GetTransactionsHandler(
    AppDbContext context,
    ICurrentUserService currentUserService,
    ITransactionSearchService searchService,
    ILogger<GetTransactionsHandler> logger) : UserAwareHandler<GetTransactionsQuery, GetTransactionsResponse>(currentUserService)
{
    public override async Task<GetTransactionsResponse> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching transactions for user {UserId}", UserId);

        var query = context.Transactions
            .Where(t => t.UserId == UserId);

        if (request.AccountId.HasValue)
            query = query.Where(t => t.AccountId == request.AccountId.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(t => t.CategoryId == request.CategoryId.Value);

        if (request.StartDate.HasValue)
            query = query.Where(t => t.TransactionDate >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(t => t.TransactionDate <= request.EndDate.Value);

        if (request.Type.HasValue)
            query = query.Where(t => t.TransactionType == request.Type.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            query = searchService.ApplySearch(query, request.SearchTerm);

        var totalCount = await query.CountAsync(cancellationToken);

        var transactions = await query
            .AsNoTracking()
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new TransactionDto(
                t.Id,
                t.Account.Name,
                t.Category != null ? t.Category.Name : null,
                t.Amount,
                t.CurrencyCode,
                t.TransactionType,
                t.PaymentType,
                t.TransactionDate,
                t.IsTransfer
                // Note and Payee excluded for security (PII data)
            ))
            .ToListAsync(cancellationToken);

        logger.LogInformation(
            "Found {Count} transactions (total {TotalCount}) for user {UserId}",
            transactions.Count,
            totalCount,
            UserId);

        return new GetTransactionsResponse(
            transactions,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}
