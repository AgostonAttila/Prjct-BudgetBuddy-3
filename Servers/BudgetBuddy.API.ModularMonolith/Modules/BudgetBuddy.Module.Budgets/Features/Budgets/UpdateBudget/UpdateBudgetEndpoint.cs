using Mapster;

namespace BudgetBuddy.Module.Budgets.Features.UpdateBudget;

public class UpdateBudgetEndpoint : ICarterModule
{
    public record UpdateBudgetRequest(
        string Name,
        decimal Amount
    );

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/budgets/{id:guid}", async (
            Guid id,
            UpdateBudgetRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<UpdateBudgetCommand>() with { Id = id };
            var result = await mediator.Send(command, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Update a budget")
        .WithDescription("Updates an existing budget's name and amount for the authenticated user.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Budgets")
        .WithName("UpdateBudget")
        ;
    }
}

