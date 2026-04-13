using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Transactions.GetTransactions;

public class GetTransactionsHandler(
    ITransactionRepository transactionRepo,
    ICurrentUserService currentUserService,
    ILogger<GetTransactionsHandler> logger) : UserAwareHandler<GetTransactionsQuery, GetTransactionsResponse>(currentUserService)
{
    public override async Task<GetTransactionsResponse> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching transactions for user {UserId}", UserId);

        var filter = new TransactionFilter(
            UserId,
            request.AccountId,
            request.CategoryId,
            request.StartDate,
            request.EndDate,
            request.Type,
            request.SearchTerm,
            request.PageNumber,
            request.PageSize);

        var (items, totalCount) = await transactionRepo.GetPagedAsync(filter, cancellationToken);

        // Note and Payee excluded for security (PII data)
        var dtos = items
            .Select(t => new TransactionDto(
                t.Id,
                t.AccountName,
                t.CategoryName,
                t.Amount,
                t.CurrencyCode,
                t.TransactionType,
                t.PaymentType,
                t.TransactionDate,
                t.IsTransfer))
            .ToList();

        logger.LogInformation(
            "Found {Count} transactions (total {TotalCount}) for user {UserId}",
            dtos.Count,
            totalCount,
            UserId);

        return new GetTransactionsResponse(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
