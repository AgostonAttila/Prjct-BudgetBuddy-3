namespace BudgetBuddy.Module.Budgets.Features.DeleteBudget;

public class DeleteBudgetEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/budgets/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteBudgetCommand(id);
            await mediator.Send(command, cancellationToken);

            return Results.NoContent();
        })
        .WithSummary("Delete a budget")
        .WithDescription("Permanently deletes a specific budget by its unique identifier.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Budgets")
        .WithName("DeleteBudget")
        ;
    }
}
