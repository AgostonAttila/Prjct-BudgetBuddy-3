using BudgetBuddy.Shared.Kernel.Constants;
using Mapster;

namespace BudgetBuddy.Module.ReferenceData.Features.Currencies.CreateCurrency;

public class CreateCurrencyEndpoint : ICarterModule
{
    public record CreateCurrencyRequest(
        string Code,
        string Symbol,
        string Name
    );

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/currencies", async (
            CreateCurrencyRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<CreateCurrencyCommand>();

            var result = await mediator.Send(command, cancellationToken);

            return Results.Created($"/api/currencies/{result.Id}", result);
        })
        .WithSummary("Create a new currency (Admin only)")
        .WithDescription("Creates a new global currency with currency code, symbol, and name. Admin role required.")
        .WithStandardRateLimit()
        .RequireAuthorization(AppPolicies.AdminOnly)
        .WithTags("Currencies")
        .WithName("CreateCurrency")
        ;
    }
}


