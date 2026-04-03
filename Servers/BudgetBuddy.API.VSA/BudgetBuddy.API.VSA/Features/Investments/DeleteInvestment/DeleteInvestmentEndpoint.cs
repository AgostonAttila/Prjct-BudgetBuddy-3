namespace BudgetBuddy.API.VSA.Features.Investments.DeleteInvestment;

public class DeleteInvestmentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/investments/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteInvestmentCommand(id);
            await mediator.Send(command, cancellationToken);

            return Results.NoContent();
        })
        .WithSummary("Delete an investment")
        .WithDescription("Permanently deletes a specific investment by its unique identifier.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Investments")
        .WithName("DeleteInvestment")
        ;
    }
}
