namespace BudgetBuddy.API.VSA.Features.Transactions.DeleteTransaction;

public class DeleteTransactionEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/transactions/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteTransactionCommand(id);
            await mediator.Send(command, cancellationToken);

            return Results.NoContent();
        })
        .WithSummary("Delete a transaction")
        .WithDescription("Permanently deletes a specific transaction by its unique identifier.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Transactions")
        .WithName("DeleteTransaction")
        ;
    }
}
