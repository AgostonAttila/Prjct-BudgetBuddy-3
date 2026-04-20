using BudgetBuddy.Shared.Kernel.Enums;

namespace BudgetBuddy.Module.Investments.Features.ExportInvestments;

public class ExportInvestmentsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/investments/export", async (
            string? type,
            string? search,
            string? format,
            ISender sender) =>
        {
            var exportFormat = format?.ToLower() == "excel" ? ExportFormat.Excel : ExportFormat.Csv;

            var query = new ExportInvestmentsQuery(
                Enum.TryParse<BudgetBuddy.Shared.Kernel.Enums.InvestmentType>(type, out var investmentType) ? investmentType : null,
                search,
                exportFormat
            );

            var result = await sender.Send(query);

            return Results.File(result.FileContent, result.ContentType, result.FileName);
        })
        .WithSummary("Export investments to file")
        .WithDescription("Exports investment data to Excel or CSV format with optional filtering by investment type and search query.")
        .WithImportExportRateLimit()
        .RequireAuthorization()
        .WithTags("Investments")
        .WithName("ExportInvestments")
        ;
    }
}
