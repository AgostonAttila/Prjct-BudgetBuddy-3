using BudgetBuddy.Shared.Infrastructure.Security;
using BudgetBuddy.Shared.Infrastructure.Security.Filescanning;
using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.Module.Transactions.Features.ImportTransactions;

public class ImportTransactionsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/transactions/import", async (
            IFormFile file,
            HttpContext httpContext,
            [FromServices] IMediator mediator,
            [FromServices] IAntivirusService antivirusService,
            [FromServices] ILogger<ImportTransactionsEndpoint> logger,
            CancellationToken cancellationToken) =>
        {
            // CSRF protection is now handled by the global antiforgery middleware
            // Client must send X-XSRF-TOKEN header with the request

            // Validate file type
            var allowedExtensions = new[] { ".xlsx", ".xls" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                return Results.BadRequest(new { error = "Invalid file type. Only Excel files (.xlsx, .xls) are allowed." });
            }

            // Validate file size (10 MB max)
            const long maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return Results.BadRequest(new { error = "File size exceeds 10 MB limit." });
            }

            // Scan file for viruses before processing
            await using var stream = file.OpenReadStream();
            var scanResult = await antivirusService.ScanAsync(stream, file.FileName, cancellationToken);

            if (scanResult.IsScanError)
            {
                logger.LogError("Antivirus scan failed for file {FileName}: {ErrorMessage}",
                    file.FileName, scanResult.ErrorMessage);
                return Results.Problem(
                    detail: "File scanning service is temporarily unavailable. Please try again later.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            if (!scanResult.IsClean)
            {
                logger.LogWarning("Malware detected in uploaded file {FileName}: {VirusName}",
                    file.FileName, scanResult.VirusName);
                return Results.BadRequest(new
                {
                    error = "File upload rejected",
                    reason = "The uploaded file contains malicious content and has been rejected for security reasons."
                });
            }

            // Reset stream position after antivirus scan
            stream.Position = 0;

            var command = new ImportTransactionsCommand(stream);
            var result = await mediator.Send(command, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Import transactions from file")
        .WithDescription("Imports transaction data from an uploaded Excel file. Validates file type, size, and scans for viruses. Requires CSRF token in X-XSRF-TOKEN header.")
        .WithImportExportRateLimit()
        .RequireAuthorization()
        .WithTags("Transactions")
        .WithName("ImportTransactions")
        ;
    }
}
