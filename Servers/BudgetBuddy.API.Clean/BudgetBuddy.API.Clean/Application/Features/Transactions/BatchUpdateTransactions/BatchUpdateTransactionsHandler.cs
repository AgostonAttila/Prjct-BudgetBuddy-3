using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Transactions.BatchUpdateTransactions;

public class BatchUpdateTransactionsHandler(
    ITransactionRepository transactionRepo,
    ICategoryRepository categoryRepo,
    IUnitOfWork uow,
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
            var category = await categoryRepo.GetByIdAsync(request.CategoryId.Value, UserId, cancellationToken);

            if (category == null)
            {
                return new BatchUpdateTransactionsResponse(
                    TotalRequested: request.TransactionIds.Count,
                    SuccessCount: 0,
                    FailedCount: request.TransactionIds.Count,
                    Errors: ["Category not found or does not belong to user"]
                );
            }
        }

        var transactions = await transactionRepo.GetByIdsAsync(request.TransactionIds, UserId, cancellationToken);

        var notFoundIds = request.TransactionIds.Except(transactions.Select(t => t.Id)).ToList();
        var errors = notFoundIds
            .Select(id => $"Transaction {id} not found or does not belong to user")
            .ToList();

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
            await uow.SaveChangesAsync(cancellationToken);
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
