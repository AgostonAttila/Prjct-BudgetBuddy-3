using BudgetBuddy.Application.Features.Investments.BatchDeleteInvestments;
using BudgetBuddy.Application.Features.Investments.CreateInvestment;
using BudgetBuddy.Application.Features.Investments.DeleteInvestment;
using BudgetBuddy.Application.Features.Investments.ExportInvestments;
using BudgetBuddy.Application.Features.Investments.GetInvestments;
using BudgetBuddy.Application.Features.Investments.UpdateInvestment;
using Mapster;
using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.API.Endpoints;

public record CreateInvestmentRequest(
    string Symbol, string Name, InvestmentType Type, decimal Quantity,
    decimal PurchasePrice, string CurrencyCode, LocalDate PurchaseDate, string? Note, Guid? AccountId);

public record UpdateInvestmentRequest(
    string Symbol, string Name, InvestmentType Type, decimal Quantity,
    decimal PurchasePrice, string CurrencyCode, LocalDate PurchaseDate, string? Note, Guid? AccountId);

public class InvestmentModule : CarterModule
{
    public InvestmentModule() : base("/api/investments")
    {
        WithTags("Investments");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", GetInvestments)
            .WithSummary("Get all investments")
            .WithDescription("Retrieves all investments for the authenticated user with optional filtering by investment type, search query, and pagination.")
            .WithStandardRateLimit()
            .CacheOutput("investments")
            .WithName("GetInvestments");

        app.MapGet("export", ExportInvestments)
            .WithSummary("Export investments to file")
            .WithDescription("Exports investment data to Excel or CSV format with optional filtering by investment type and search query.")
            .WithImportExportRateLimit()
            .WithName("ExportInvestments");

        app.MapPost("", CreateInvestment)
            .WithSummary("Create a new investment")
            .WithDescription("Creates a new investment record. Include an Idempotency-Key header to prevent duplicate purchases.")
            .WithIdempotency()
            .WithStandardRateLimit()
            .WithName("CreateInvestment");

        app.MapPut("{id:guid}", UpdateInvestment)
            .WithSummary("Update an investment")
            .WithDescription("Updates an existing investment's details including symbol, quantity, purchase price, and date.")
            .WithStandardRateLimit()
            .WithName("UpdateInvestment");

        app.MapDelete("{id:guid}", DeleteInvestment)
            .WithSummary("Delete an investment")
            .WithDescription("Permanently deletes a specific investment by its unique identifier.")
            .WithStandardRateLimit()
            .WithName("DeleteInvestment");

        app.MapDelete("batch", BatchDeleteInvestments)
            .WithSummary("Batch delete investments")
            .WithDescription("Deletes multiple investments in a single operation. Returns success and failure counts for partial operations.")
            .WithBatchOperationRateLimit()
            .WithName("BatchDeleteInvestments")
            .Produces<BatchDeleteInvestmentsResponse>(200)
            .Produces<BatchDeleteInvestmentsResponse>(400);
    }

    private static async Task<IResult> GetInvestments(IMediator mediator, InvestmentType? type, string? search, int pageNumber = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetInvestmentsQuery(type, search, pageNumber, pageSize), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> ExportInvestments(string? type, string? search, string? format, ISender sender)
    {
        var exportFormat = format?.ToLower() == "excel" ? ExportFormat.Excel : ExportFormat.Csv;
        var query = new ExportInvestmentsQuery(
            Enum.TryParse<InvestmentType>(type, out var investmentType) ? investmentType : null,
            search,
            exportFormat);
        var result = await sender.Send(query);
        return Results.File(result.FileContent, result.ContentType, result.FileName);
    }

    private static async Task<IResult> CreateInvestment(CreateInvestmentRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateInvestmentCommand>();
        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/investments/{result.Id}", result);
    }

    private static async Task<IResult> UpdateInvestment(Guid id, UpdateInvestmentRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateInvestmentCommand>() with { Id = id };
        var result = await mediator.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteInvestment(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteInvestmentCommand(id), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> BatchDeleteInvestments([FromBody] BatchDeleteInvestmentsCommand command, ISender sender)
    {
        var result = await sender.Send(command);
        return result.SuccessCount == 0 ? Results.BadRequest(result) : Results.Ok(result);
    }
}
