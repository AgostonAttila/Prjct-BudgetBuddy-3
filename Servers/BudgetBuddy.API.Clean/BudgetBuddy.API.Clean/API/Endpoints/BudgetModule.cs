using BudgetBuddy.Application.Features.Budgets.CreateBudget;
using BudgetBuddy.Application.Features.Budgets.DeleteBudget;
using BudgetBuddy.Application.Features.Budgets.GetBudgetVsActual;
using BudgetBuddy.Application.Features.Budgets.GetBudgets;
using BudgetBuddy.Application.Features.Budgets.UpdateBudget;
using Mapster;

namespace BudgetBuddy.API.Endpoints;

public record CreateBudgetRequest(string Name, Guid CategoryId, decimal Amount, string CurrencyCode, int Year, int Month);
public record UpdateBudgetRequest(string Name, decimal Amount);

public class BudgetModule : CarterModule
{
    public BudgetModule() : base("/api/budgets")
    {
        WithTags("Budgets");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", GetBudgets)
            .WithSummary("Get all budgets")
            .WithDescription("Retrieves all budgets for the authenticated user with optional filtering by year, month, and category.")
            .WithStandardRateLimit()
            .CacheOutput("budgets-list")
            .WithName("GetBudgets");

        app.MapGet("vs-actual", GetBudgetVsActual)
            .WithSummary("Get budget vs actual spending")
            .WithDescription("Compares budgeted amounts against actual spending for all categories in a specific month and year.")
            .WithStandardRateLimit()
            .CacheOutput("budget-vs-actual")
            .WithName("GetBudgetVsActual");

        app.MapPost("", CreateBudget)
            .WithSummary("Create a new budget")
            .WithDescription("Creates a new budget for a specific category and time period. Include an Idempotency-Key header to prevent duplicates.")
            .WithIdempotency()
            .WithStandardRateLimit()
            .WithName("CreateBudget");

        app.MapPut("{id:guid}", UpdateBudget)
            .WithSummary("Update a budget")
            .WithDescription("Updates an existing budget's name and amount for the authenticated user.")
            .WithStandardRateLimit()
            .WithName("UpdateBudget");

        app.MapDelete("{id:guid}", DeleteBudget)
            .WithSummary("Delete a budget")
            .WithDescription("Permanently deletes a specific budget by its unique identifier.")
            .WithStandardRateLimit()
            .WithName("DeleteBudget");
    }

    private static async Task<IResult> GetBudgets(IMediator mediator, int? year, int? month, Guid? categoryId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBudgetsQuery(year, month, categoryId), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetBudgetVsActual(IMediator mediator, int year, int month, CancellationToken cancellationToken)
    {
        if (year < 2000 || year > 2100)
            return Results.BadRequest("Invalid year");

        if (month < 1 || month > 12)
            return Results.BadRequest("Invalid month");

        var result = await mediator.Send(new GetBudgetVsActualQuery(year, month), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateBudget(CreateBudgetRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateBudgetCommand>();
        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/budgets/{result.Id}", result);
    }

    private static async Task<IResult> UpdateBudget(Guid id, UpdateBudgetRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateBudgetCommand>() with { Id = id };
        var result = await mediator.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteBudget(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteBudgetCommand(id), cancellationToken);
        return Results.NoContent();
    }
}
