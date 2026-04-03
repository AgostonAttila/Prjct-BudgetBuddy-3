namespace BudgetBuddy.API.VSA.Features.CategoryTypes.UpdateCategoryType;

public class UpdateCategoryTypeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/category-types/{id:guid}", async (
            Guid id,
            UpdateCategoryTypeRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateCategoryTypeCommand(id, request.Name, request.Icon, request.Color);

            await mediator.Send(command, cancellationToken);
            return Results.NoContent();
        })
        .WithSummary("Update a category type")
        .WithDescription("Updates an existing category type's information.")
        .RequireAuthorization()
        .WithTags("Category Types")
        .WithName("UpdateCategoryType")
        .Produces(204)
        .Produces(400)
        .Produces(404)
        ;
    }
}
