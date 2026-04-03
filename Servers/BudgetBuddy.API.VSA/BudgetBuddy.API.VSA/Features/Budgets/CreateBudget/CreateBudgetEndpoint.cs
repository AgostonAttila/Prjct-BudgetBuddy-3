using Mapster;

namespace BudgetBuddy.API.VSA.Features.Budgets.CreateBudget;

public class CreateBudgetEndpoint : ICarterModule
{
    public record CreateBudgetRequest(
        string Name,
        Guid CategoryId,
        decimal Amount,
        string CurrencyCode,
        int Year,
        int Month
    );

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/budgets", async (
            CreateBudgetRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<CreateBudgetCommand>();

            var result = await mediator.Send(command, cancellationToken);

            return Results.Created($"/api/budgets/{result.Id}", result);
        })
        .WithSummary("Create a new budget")
        .WithDescription("Creates a new budget for a specific category and time period. Include an Idempotency-Key header to prevent duplicates.")
        .WithIdempotency()  // Prevent duplicate budgets
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Budgets")
        .WithName("CreateBudget")
        ;
    }
}


