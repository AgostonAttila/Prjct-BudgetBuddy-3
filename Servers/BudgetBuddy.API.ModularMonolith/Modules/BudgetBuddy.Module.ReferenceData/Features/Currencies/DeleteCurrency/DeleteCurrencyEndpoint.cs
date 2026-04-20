using BudgetBuddy.Shared.Kernel.Constants;

namespace BudgetBuddy.Module.ReferenceData.Features.Currencies.DeleteCurrency;

public class DeleteCurrencyEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/currencies/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteCurrencyCommand(id);
            await mediator.Send(command, cancellationToken);

            return Results.NoContent();
        })
        .WithSummary("Delete a currency (Admin only)")
        .WithDescription("Permanently deletes a specific global currency by its unique identifier. Cannot delete if in use by accounts. Admin role required.")
        .WithStandardRateLimit()
        .RequireAuthorization(AppPolicies.AdminOnly)
        .WithTags("Currencies")
        .WithName("DeleteCurrency")
        ;
    }
}
