using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using EFCore.BulkExtensions;

namespace BudgetBuddy.Module.Transactions.Features.BatchUpdateTransactions;

public class BatchUpdateTransactionsHandler(
    TransactionsDbContext context,
    ICategoryQueryService categoryQueryService,
    ICurrentUserService currentUserService,
    ILogger<BatchUpdateTransactionsHandler> logger) : UserAwareHandler<BatchUpdateTransactionsCommand, BatchUpdateTransactionsResponse>(currentUserService)
{
    public override async Task<BatchUpdateTransactionsResponse> Handle(
        BatchUpdateTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Starting batch update for {Count} transactions for user {UserId}",
            request.TransactionIds.Count,
            UserId);

        // Validate category if provided
        if (request.CategoryId.HasValue)
        {
            var cats = await categoryQueryService.GetCategoriesByIdsAsync(
                [request.CategoryId.Value], cancellationToken);

            if (!cats.ContainsKey(request.CategoryId.Value))
            {
                return new BatchUpdateTransactionsResponse(
                    TotalRequested: request.TransactionIds.Count,
                    SuccessCount: 0,
                    FailedCount: request.TransactionIds.Count,
                    Errors: ["Category not found or does not belong to user"]
                );
            }
        }

        // Get all transactions to update
        var transactions = await context.Transactions
            .Where(t => request.TransactionIds.Contains(t.Id) && t.UserId == UserId)
            .ToListAsync(cancellationToken);

        var notFoundIds = request.TransactionIds.Except(transactions.Select(t => t.Id)).ToList();
        var errors = notFoundIds
            .Select(id => $"Transaction {id} not found or does not belong to user")
            .ToList();

        // Apply updates to all transactions
        foreach (var transaction in transactions)
        {
            if (request.CategoryId.HasValue)
                transaction.CategoryId = request.CategoryId.Value;

            if (!string.IsNullOrWhiteSpace(request.Labels))
                transaction.Labels = request.Labels;
        }

        var successCount = 0;

        if (transactions.Count > 0)
        {
            await context.BulkUpdateAsync(transactions, cancellationToken: cancellationToken);
            successCount = transactions.Count;
        }

        logger.LogInformation(
            "Batch update completed: {SuccessCount}/{TotalRequested} transactions updated",
            successCount,
            request.TransactionIds.Count);

        return new BatchUpdateTransactionsResponse(
            TotalRequested: request.TransactionIds.Count,
            SuccessCount: successCount,
            FailedCount: request.TransactionIds.Count - successCount,
            Errors: errors
        );
    }
}
