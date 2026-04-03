namespace BudgetBuddy.API.VSA.Features.CategoryTypes.DeleteCategoryType;

public class DeleteCategoryTypeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapDelete("/api/category-types/{id:guid}", async (
            Guid id,
            ISender sender) =>
        {
            var command = new DeleteCategoryTypeCommand(id);
            await sender.Send(command);

            return Results.NoContent();
        })
        .WithSummary("Delete a category type")
        .WithDescription("Permanently deletes a specific category type by its unique identifier.")
        .RequireAuthorization()
        .WithTags("Category Types")
        .WithName("DeleteCategoryType")
        .Produces(204)
        .Produces(404)
        ;
    }
}
