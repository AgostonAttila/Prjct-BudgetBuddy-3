using BudgetBuddy.API.VSA.Common.Domain.Constants;
using Mapster;

namespace BudgetBuddy.API.VSA.Features.Currencies.UpdateCurrency;

public class UpdateCurrencyEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/currencies/{id:guid}", async (
            Guid id,
            UpdateCurrencyRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<UpdateCurrencyCommand>() with { Id = id };

            var result = await mediator.Send(command, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Update a currency (Admin only)")
        .WithDescription("Updates an existing global currency's code, symbol, and name properties. Admin role required.")
        .WithStandardRateLimit()
        .RequireAuthorization(AppPolicies.AdminOnly)
        .WithTags("Currencies")
        .WithName("UpdateCurrency")
        ;
    }
}

public record UpdateCurrencyRequest(
    string Code,
    string Symbol,
    string Name
);
