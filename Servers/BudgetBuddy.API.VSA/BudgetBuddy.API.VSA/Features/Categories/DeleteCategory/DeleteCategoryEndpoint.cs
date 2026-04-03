namespace BudgetBuddy.API.VSA.Features.Categories.DeleteCategory;

public class DeleteCategoryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/categories/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteCategoryCommand(id);
            await mediator.Send(command, cancellationToken);

            return Results.NoContent();
        })
        .WithSummary("Delete a category")
        .WithDescription("Permanently deletes a specific category by its unique identifier. Warning: This may affect related transactions and budgets.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Categories")
        .WithName("DeleteCategory")
        ;
    }
}
