using BudgetBuddy.Application.Features.Currencies.CreateCurrency;
using BudgetBuddy.Application.Features.Currencies.DeleteCurrency;
using BudgetBuddy.Application.Features.Currencies.GetCurrencies;
using BudgetBuddy.Application.Features.Currencies.UpdateCurrency;
using Mapster;

namespace BudgetBuddy.API.Endpoints;

public record CreateCurrencyRequest(string Code, string Symbol, string Name);
public record UpdateCurrencyRequest(string Code, string Symbol, string Name);

public class CurrencyModule : CarterModule
{
    public CurrencyModule() : base("/api/currencies")
    {
        WithTags("Currencies");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", GetCurrencies)
            .WithSummary("Get all currencies")
            .WithDescription("Retrieves all currencies available to the authenticated user.")
            .WithStandardRateLimit()
            .CacheOutput("currencies")
            .WithName("GetCurrencies");

        app.MapPost("", CreateCurrency)
            .WithSummary("Create a new currency (Admin only)")
            .WithDescription("Creates a new global currency with currency code, symbol, and name. Admin role required.")
            .WithStandardRateLimit()
            .RequireAuthorization(AppPolicies.AdminOnly)
            .WithName("CreateCurrency");

        app.MapPut("{id:guid}", UpdateCurrency)
            .WithSummary("Update a currency (Admin only)")
            .WithDescription("Updates an existing global currency's code, symbol, and name properties. Admin role required.")
            .WithStandardRateLimit()
            .RequireAuthorization(AppPolicies.AdminOnly)
            .WithName("UpdateCurrency");

        app.MapDelete("{id:guid}", DeleteCurrency)
            .WithSummary("Delete a currency (Admin only)")
            .WithDescription("Permanently deletes a specific global currency by its unique identifier. Cannot delete if in use by accounts. Admin role required.")
            .WithStandardRateLimit()
            .RequireAuthorization(AppPolicies.AdminOnly)
            .WithName("DeleteCurrency");
    }

    private static async Task<IResult> GetCurrencies(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCurrenciesQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateCurrency(CreateCurrencyRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateCurrencyCommand>();
        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/currencies/{result.Id}", result);
    }

    private static async Task<IResult> UpdateCurrency(Guid id, UpdateCurrencyRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateCurrencyCommand>() with { Id = id };
        var result = await mediator.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteCurrency(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteCurrencyCommand(id), cancellationToken);
        return Results.NoContent();
    }
}
