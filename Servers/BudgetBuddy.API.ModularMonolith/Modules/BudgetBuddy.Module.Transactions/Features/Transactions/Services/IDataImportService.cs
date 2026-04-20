namespace BudgetBuddy.Module.Transactions.Features.Transactions.Services;

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
