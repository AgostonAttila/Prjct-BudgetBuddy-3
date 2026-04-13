using BudgetBuddy.Application.Common.Contracts;
using BudgetBuddy.Application.Common.Handlers;

namespace BudgetBuddy.Application.Features.Transactions.ImportTransactions;

public class ImportTransactionsHandler(
    IDataImportService importService,
    ICurrentUserService currentUserService,
    ILogger<ImportTransactionsHandler> logger) : UserAwareHandler<ImportTransactionsCommand, ImportResult>(currentUserService)
{
    public override async Task<ImportResult> Handle(
        ImportTransactionsCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Importing transactions for user {UserId}",
            UserId);

        var result = await importService.ImportTransactionsAsync(
            request.FileStream,
            UserId,
            cancellationToken);

        logger.LogInformation(
            "Import completed. Success: {SuccessCount}, Errors: {ErrorCount}",
            result.SuccessCount,
            result.ErrorCount);

        return result;
    }
}
