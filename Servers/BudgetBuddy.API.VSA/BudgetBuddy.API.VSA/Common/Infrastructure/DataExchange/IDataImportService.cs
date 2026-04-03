

namespace BudgetBuddy.API.VSA.Common.Infrastructure.DataExchange;

public interface IDataImportService
{
    Task<ImportResult> ImportTransactionsAsync(
        Stream fileStream,
        string userId,
        CancellationToken cancellationToken = default);
}

public record ImportResult(
    int TotalRows,
    int SuccessCount,
    int ErrorCount,
    List<string> Errors,
    List<Transaction> ImportedTransactions
);
