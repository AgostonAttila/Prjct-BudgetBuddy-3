using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Infrastructure.DataExchange;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;

namespace BudgetBuddy.API.VSA.Features.Transactions.ImportTransactions;

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
