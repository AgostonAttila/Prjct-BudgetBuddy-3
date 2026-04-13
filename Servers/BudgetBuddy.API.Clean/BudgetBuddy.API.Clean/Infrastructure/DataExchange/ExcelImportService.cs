using ClosedXML.Excel;
using EFCore.BulkExtensions;

namespace BudgetBuddy.Infrastructure.DataExchange;

public class ExcelImportService(AppDbContext context, IClock clock, ILogger<ExcelImportService> logger) : IDataImportService
{


    public async Task<ImportResult> ImportTransactionsAsync(
        Stream fileStream,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var allTransactions = new List<Transaction>(); // For returning in result

        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);

        var rows = worksheet.RowsUsed().Skip(1); // Skip header

        // Cache user's data for performance (load once, reuse for all batches)
        var accounts = await context.Accounts
            .Where(a => a.UserId == userId)
            .ToDictionaryAsync(a => a.Name, cancellationToken);

        var categories = await context.Categories
            .Where(c => c.UserId == userId)
            .ToDictionaryAsync(c => c.Name, cancellationToken);

        // BATCH PROCESSING: Process and insert rows in batches to optimize memory usage
        const int batchSize = 500; // Process 500 rows at a time
        const int maxRows = 10000; // Maximum total rows allowed
        var rowNumber = 2; // Excel row numbering starts at 1, header is row 1
        var totalProcessed = 0;
        var batch = new List<Transaction>(batchSize);

        using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var row in rows)
            {
                // Validate row count to prevent DoS from extremely large files
                if (totalProcessed >= maxRows)
                {
                    throw new DomainValidationException($"File contains too many rows. Maximum allowed is {maxRows}.");
                }

                try
                {
                    var parsedTransaction = ParseRow(row, userId, accounts, categories);
                    batch.Add(parsedTransaction);
                    allTransactions.Add(parsedTransaction);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to parse row {RowNumber}", rowNumber);
                    errors.Add($"Row {rowNumber}: {ex.Message}");
                }

                rowNumber++;
                totalProcessed++;

                // When batch is full, insert it
                if (batch.Count >= batchSize)
                {
                    logger.LogInformation(
                        "Batch inserting {Count} transactions (rows {Start}-{End})",
                        batch.Count, rowNumber - batch.Count, rowNumber - 1);

                    await context.BulkInsertAsync(batch, cancellationToken: cancellationToken);
                    batch.Clear(); // Free memory
                }
            }

            // Insert remaining transactions in final batch
            if (batch.Count > 0)
            {
                logger.LogInformation(
                    "Final batch inserting {Count} transactions",
                    batch.Count);

                await context.BulkInsertAsync(batch, cancellationToken: cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Successfully imported {Count} transactions in {Batches} batches",
                allTransactions.Count,
                (int)Math.Ceiling(allTransactions.Count / (double)batchSize));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import failed. Rolling back all transactions");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new ImportResult(
            totalProcessed,
            allTransactions.Count,
            errors.Count,
            errors,
            allTransactions
        );
    }

    private Transaction ParseRow(
        IXLRow row,
        string userId,
        Dictionary<string, Account> accounts,
        Dictionary<string, Category> categories)
    {
        // Excel columns: account, category, currency, amount, ref_currency_amount,
        // type, payment_type, note, date, transfer, payee, labels

        var accountName = row.Cell(1).GetString();
        var categoryName = row.Cell(2).GetString();
        var currencyCode = row.Cell(3).GetString();
        var amount = row.Cell(4).GetValue<decimal>();
        var refAmount = row.Cell(5).GetValue<decimal?>();
        var typeStr = row.Cell(6).GetString(); // Hungarian: "Kiadás" / "Bevétel"
        var paymentTypeStr = row.Cell(7).GetString(); // Hungarian: "Betéti kártya"
        var note = row.Cell(8).GetString();
        var dateStr = row.Cell(9).GetString();
        var isTransfer = row.Cell(10).GetValue<bool>();
        var payee = row.Cell(11).GetString();
        var labels = row.Cell(12).GetString();

        if (!accounts.TryGetValue(accountName, out var account))
        {
            throw new Exception($"Account '{accountName}' not found. Please create account first.");
        }
    
        if (!DateTime.TryParse(dateStr, out var dateTime))
        {
            throw new Exception($"Invalid date format: '{dateStr}'. Expected ISO 8601 format (e.g., 2022-12-31T00:00:00.000Z).");
        }
        var localDate = LocalDate.FromDateTime(dateTime);

        // Map Hungarian text to enums (TODO: Use localization service)
        var transactionType = MapTransactionType(typeStr);
        var paymentType = MapPaymentType(paymentTypeStr);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            CategoryId = categories.ContainsKey(categoryName) ? categories[categoryName].Id : null,
            Amount = amount,
            CurrencyCode = currencyCode,
            RefCurrencyAmount = refAmount,
            TransactionType = transactionType,
            PaymentType = paymentType,
            Note = note,
            TransactionDate = localDate,
            IsTransfer = isTransfer,
            Payee = payee,
            Labels = labels,
            UserId = userId,
            CreatedAt = clock.GetCurrentInstant()
        };

        return transaction;
    }

    private static TransactionType MapTransactionType(string hungarianText)
    {
        // TODO: Move to localization service
        return hungarianText.ToLower() switch
        {
            "kiadás" => TransactionType.Expense,
            "bevétel" => TransactionType.Income,
            "átutalás" => TransactionType.Transfer,
            "expense" => TransactionType.Expense,
            "income" => TransactionType.Income,
            "transfer" => TransactionType.Transfer,
            _ => throw new Exception($"Unknown transaction type: {hungarianText}")
        };
    }

    private static PaymentType MapPaymentType(string hungarianText)
    {
        // TODO: Move to localization service
        return hungarianText.ToLower() switch
        {
            "betéti kártya" => PaymentType.Card,
            "készpénz" => PaymentType.Cash,
            "banki átutalás" => PaymentType.BankTransfer,
            "card" => PaymentType.Card,
            "cash" => PaymentType.Cash,
            "bank transfer" => PaymentType.BankTransfer,
            _ => PaymentType.Other
        };
    }
}
