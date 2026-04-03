using BudgetBuddy.API.VSA.Common.Domain.Enums;

namespace BudgetBuddy.API.VSA.Features.Transactions.ExportTransactions;

public class ExportTransactionsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/transactions/export", async (
            Guid? accountId,
            Guid? categoryId,
            string? transactionType,
            string? startDate,
            string? endDate,
            string? search,
            string? format,
            ISender sender) =>
        {
            var exportFormat = format?.ToLower() == "excel" ? ExportFormat.Excel : ExportFormat.Csv;

            var query = new ExportTransactionsQuery(
                accountId,
                categoryId,
                Enum.TryParse<Common.Domain.Enums.TransactionType>(transactionType, out var type) ? type : null,
                startDate,
                endDate,
                search,
                exportFormat
            );

            var result = await sender.Send(query);

            return Results.File(result.FileContent, result.ContentType, result.FileName);
        })
        .WithSummary("Export transactions to file")
        .WithDescription("Exports transaction data to Excel or CSV format with optional filtering by account, category, date range, and transaction type.")
        .WithImportExportRateLimit()
        .RequireAuthorization()
        .WithTags("Transactions")
        .WithName("ExportTransactions")
        ;
    }
}
