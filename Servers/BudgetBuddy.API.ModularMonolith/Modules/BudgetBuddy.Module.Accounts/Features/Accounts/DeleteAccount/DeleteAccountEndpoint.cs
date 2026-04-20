

namespace BudgetBuddy.Module.Accounts.Features.DeleteAccount;

public class DeleteAccountEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/accounts/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteAccountCommand(id);
            await mediator.Send(command, cancellationToken);

            return Results.NoContent();
        })
        .WithSummary("Delete an account")
        .WithDescription("Permanently deletes an account. Warning: This will also delete all associated transactions.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Accounts")
        .WithName("DeleteAccount");
    }
}
