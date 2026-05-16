using BudgetBuddy.Service.Transactions.Features.Transactions.Services;
using BudgetBuddy.Service.Transactions.ReadModels;
using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Service.Transactions.Features.GetTransactions;

public class GetTransactionsHandler(
    TransactionsDbContext context,
    ICurrentUserService currentUserService,
    ITransactionSearchService searchService,
    ICategoryQueryService categoryQueryService,
    ILogger<GetTransactionsHandler> logger) : UserAwareHandler<GetTransactionsQuery, GetTransactionsResponse>(currentUserService)
{
    public override async Task<GetTransactionsResponse> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching transactions for user {UserId}", UserId);

        var query = context.Transactions
            .Where(t => t.UserId == UserId && !t.IsHidden);

        if (request.AccountId.HasValue)
        {
            query = query.Where(t => t.AccountId == request.AccountId.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == request.CategoryId.Value);
        }

        if (request.StartDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= request.EndDate.Value);
        }

        if (request.Type.HasValue)
        {
            query = query.Where(t => t.TransactionType == request.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = searchService.ApplySearch(query, request.SearchTerm);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var rawTransactions = await query
            .AsNoTracking()
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new { t.Id, t.AccountId, t.CategoryId, t.Amount, t.CurrencyCode, t.TransactionType, t.PaymentType, t.TransactionDate, t.IsTransfer })
            .ToListAsync(cancellationToken);

        var categoryIds = rawTransactions
            .Where(t => t.CategoryId.HasValue)
            .Select(t => t.CategoryId!.Value)
            .Distinct();

        var categoryNames = await categoryQueryService.GetCategoriesByIdsAsync(categoryIds, cancellationToken);

        var transactions = rawTransactions.Select(t => new TransactionDto(
            t.Id,
            string.Empty, // AccountName: cross-module navigation removed — use AccountId for lookups
            t.CategoryId.HasValue && categoryNames.TryGetValue(t.CategoryId.Value, out var cat) ? cat.Name : null,
            t.Amount,
            t.CurrencyCode,
            t.TransactionType,
            t.PaymentType,
            t.TransactionDate,
            t.IsTransfer
            // Note and Payee excluded for security (PII data)
        )).ToList();

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
