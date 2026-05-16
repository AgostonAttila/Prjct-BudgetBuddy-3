using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.Service.ReferenceData.Features.CategoryTypes.CreateCategoryType;

public class CreateCategoryTypeEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/category-types", async (
            [FromBody] CreateCategoryTypeCommand command,
            ISender sender) =>
        {
            var result = await sender.Send(command);
            return Results.Created($"/api/category-types/{result.Id}", result);
        })
        .WithSummary("Create a new category type")
        .WithDescription("Creates a new category type for organizing and classifying transaction categories. Admin role required.")
        .RequireAuthorization(AppPolicies.AdminOnly)
        .WithTags("Category Types")
        .WithName("CreateCategoryType")
        .Produces<CreateCategoryTypeResponse>(201)
        .Produces(400)
        ;
    }
}
