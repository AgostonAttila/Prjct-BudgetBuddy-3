using BudgetBuddy.Application.Features.Transactions.BatchDeleteTransactions;
using BudgetBuddy.Application.Features.Transactions.BatchUpdateTransactions;
using BudgetBuddy.Application.Features.Transactions.CreateTransaction;
using BudgetBuddy.Application.Features.Transactions.DeleteTransaction;
using BudgetBuddy.Application.Features.Transactions.ExportTransactions;
using BudgetBuddy.Application.Features.Transactions.GetTransactions;
using BudgetBuddy.Application.Features.Transactions.ImportTransactions;
using BudgetBuddy.Application.Features.Transactions.UpdateTransaction;
using BudgetBuddy.Infrastructure.Security.Filescanning;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using NodaTime.Text;

namespace BudgetBuddy.API.Endpoints;

public record CreateTransactionRequest(
    Guid AccountId, Guid? CategoryId, Guid? TypeId, decimal Amount, string CurrencyCode,
    decimal? RefCurrencyAmount, TransactionType TransactionType, PaymentType PaymentType,
    string? Note, LocalDate TransactionDate, bool IsTransfer = false,
    Guid? TransferToAccountId = null, string? Payee = null, string? Labels = null);

public record UpdateTransactionRequest(
    Guid? CategoryId, Guid? TypeId, decimal Amount, string CurrencyCode,
    decimal? RefCurrencyAmount, TransactionType TransactionType, PaymentType PaymentType,
    string? Note, LocalDate TransactionDate, string? Payee, string? Labels);

public class TransactionModule : CarterModule
{
    public TransactionModule() : base("/api/transactions")
    {
        WithTags("Transactions");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", GetTransactions)
            .WithSummary("Get all transactions")
            .WithDescription("Retrieves all transactions for the authenticated user with optional filtering by account, category, date range, and transaction type.")
            .WithStandardRateLimit()
            .CacheOutput("transactions")
            .WithName("GetTransactions");

        app.MapGet("export", ExportTransactions)
            .WithSummary("Export transactions to file")
            .WithDescription("Exports transaction data to Excel or CSV format with optional filtering by account, category, date range, and transaction type.")
            .WithImportExportRateLimit()
            .WithName("ExportTransactions");

        app.MapPost("", CreateTransaction)
            .WithSummary("Create a new transaction")
            .WithDescription("Creates a new financial transaction. Include an Idempotency-Key header to prevent duplicate transactions on retry.")
            .WithIdempotency()
            .WithStandardRateLimit()
            .WithName("CreateTransaction");

        app.MapPost("import", ImportTransactions)
            .WithSummary("Import transactions from file")
            .WithDescription("Imports transaction data from an uploaded Excel file. Validates file type, size, and scans for viruses. Requires CSRF token in X-XSRF-TOKEN header.")
            .WithImportExportRateLimit()
            .WithName("ImportTransactions");

        app.MapPut("{id:guid}", UpdateTransaction)
            .WithSummary("Update a transaction")
            .WithDescription("Updates an existing transaction's details including amount, category, payment type, and transaction date.")
            .WithStandardRateLimit()
            .WithName("UpdateTransaction");

        app.MapDelete("{id:guid}", DeleteTransaction)
            .WithSummary("Delete a transaction")
            .WithDescription("Permanently deletes a specific transaction by its unique identifier.")
            .WithStandardRateLimit()
            .WithName("DeleteTransaction");

        app.MapDelete("batch", BatchDeleteTransactions)
            .WithSummary("Batch delete transactions")
            .WithDescription("Deletes multiple transactions in a single operation. Returns success and failure counts for partial operations.")
            .WithBatchOperationRateLimit()
            .WithName("BatchDeleteTransactions")
            .Produces<BatchDeleteTransactionsResponse>(200)
            .Produces<BatchDeleteTransactionsResponse>(400);

        app.MapPatch("batch", BatchUpdateTransactions)
            .WithSummary("Batch update transactions")
            .WithDescription("Updates multiple transactions in a single operation. Returns success and failure counts for partial operations.")
            .WithBatchOperationRateLimit()
            .WithName("BatchUpdateTransactions")
            .Produces<BatchUpdateTransactionsResponse>(200)
            .Produces<BatchUpdateTransactionsResponse>(400);
    }

    private static async Task<IResult> GetTransactions(
        IMediator mediator, Guid? accountId, Guid? categoryId, string? startDate, string? endDate,
        TransactionType? type, string? search, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        LocalDate? parsedStartDate = null;
        LocalDate? parsedEndDate = null;

        if (!string.IsNullOrEmpty(startDate))
        {
            var r = LocalDatePattern.Iso.Parse(startDate);
            if (r.Success) parsedStartDate = r.Value;
        }

        if (!string.IsNullOrEmpty(endDate))
        {
            var r = LocalDatePattern.Iso.Parse(endDate);
            if (r.Success) parsedEndDate = r.Value;
        }

        var result = await mediator.Send(new GetTransactionsQuery(accountId, categoryId, parsedStartDate, parsedEndDate, type, search, pageNumber, pageSize), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> ExportTransactions(
        Guid? accountId, Guid? categoryId, string? transactionType, string? startDate, string? endDate, string? search, string? format, ISender sender)
    {
        var exportFormat = format?.ToLower() == "excel" ? ExportFormat.Excel : ExportFormat.Csv;
        var query = new ExportTransactionsQuery(
            accountId, categoryId,
            Enum.TryParse<TransactionType>(transactionType, out var type) ? type : null,
            startDate, endDate, search, exportFormat);
        var result = await sender.Send(query);
        return Results.File(result.FileContent, result.ContentType, result.FileName);
    }

    private static async Task<IResult> CreateTransaction(CreateTransactionRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateTransactionCommand>();
        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/transactions/{result.Id}", result);
    }

    private static async Task<IResult> ImportTransactions(
        IFormFile file, HttpContext httpContext, IMediator mediator,
        IAntivirusService antivirusService, ILogger<TransactionModule> logger, CancellationToken cancellationToken)
    {
        var allowedExtensions = new[] { ".xlsx", ".xls" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
            return Results.BadRequest(new { error = "Invalid file type. Only Excel files (.xlsx, .xls) are allowed." });

        const long maxFileSize = 10 * 1024 * 1024;
        if (file.Length > maxFileSize)
            return Results.BadRequest(new { error = "File size exceeds 10 MB limit." });

        await using var stream = file.OpenReadStream();
        var scanResult = await antivirusService.ScanAsync(stream, file.FileName, cancellationToken);

        if (scanResult.IsScanError)
        {
            logger.LogError("Antivirus scan failed for file {FileName}: {ErrorMessage}", file.FileName, scanResult.ErrorMessage);
            return Results.Problem(detail: "File scanning service is temporarily unavailable. Please try again later.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (!scanResult.IsClean)
        {
            logger.LogWarning("Malware detected in uploaded file {FileName}: {VirusName}", file.FileName, scanResult.VirusName);
            return Results.BadRequest(new { error = "File upload rejected", reason = "The uploaded file contains malicious content and has been rejected for security reasons." });
        }

        stream.Position = 0;
        var result = await mediator.Send(new ImportTransactionsCommand(stream), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateTransaction(Guid id, UpdateTransactionRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateTransactionCommand>() with { Id = id };
        var result = await mediator.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteTransaction(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteTransactionCommand(id), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> BatchDeleteTransactions([FromBody] BatchDeleteTransactionsCommand command, ISender sender)
    {
        var result = await sender.Send(command);
        return result.SuccessCount == 0 ? Results.BadRequest(result) : Results.Ok(result);
    }

    private static async Task<IResult> BatchUpdateTransactions([FromBody] BatchUpdateTransactionsCommand command, ISender sender)
    {
        var result = await sender.Send(command);
        return result.SuccessCount == 0 ? Results.BadRequest(result) : Results.Ok(result);
    }
}
