using BudgetBuddy.Application.Features.CategoryTypes.CreateCategoryType;
using BudgetBuddy.Application.Features.CategoryTypes.DeleteCategoryType;
using BudgetBuddy.Application.Features.CategoryTypes.GetCategoryTypes;
using BudgetBuddy.Application.Features.CategoryTypes.UpdateCategoryType;
using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.API.Endpoints;

public record UpdateCategoryTypeRequest(string Name, string? Icon, string? Color);

public class CategoryTypeModule : CarterModule
{
    public CategoryTypeModule() : base("/api/category-types")
    {
        WithTags("Category Types");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", GetCategoryTypes)
            .WithSummary("Get all category types")
            .WithDescription("Retrieves all category types with optional filtering by category and pagination support.")
            .CacheOutput("category-types")
            .WithName("GetCategoryTypes")
            .Produces<GetCategoryTypesResponse>(200);

        app.MapPost("", CreateCategoryType)
            .WithSummary("Create a new category type")
            .WithDescription("Creates a new category type for organizing and classifying transaction categories.")
            .WithName("CreateCategoryType")
            .Produces<CreateCategoryTypeResponse>(201)
            .Produces(400);

        app.MapPut("{id:guid}", UpdateCategoryType)
            .WithSummary("Update a category type")
            .WithDescription("Updates an existing category type's information.")
            .WithName("UpdateCategoryType")
            .Produces(204)
            .Produces(400)
            .Produces(404);

        app.MapDelete("{id:guid}", DeleteCategoryType)
            .WithSummary("Delete a category type")
            .WithDescription("Permanently deletes a specific category type by its unique identifier.")
            .WithName("DeleteCategoryType")
            .Produces(204)
            .Produces(404);
    }

    private static async Task<IResult> GetCategoryTypes(ISender sender, Guid? categoryId, int page = 1, int pageSize = 10)
    {
        var result = await sender.Send(new GetCategoryTypesQuery(categoryId, page, pageSize));
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateCategoryType([FromBody] CreateCategoryTypeCommand command, ISender sender)
    {
        var result = await sender.Send(command);
        return Results.Created($"/api/category-types/{result.Id}", result);
    }

    private static async Task<IResult> UpdateCategoryType(Guid id, UpdateCategoryTypeRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = new UpdateCategoryTypeCommand(id, request.Name, request.Icon, request.Color);
        await mediator.Send(command, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteCategoryType(Guid id, ISender sender)
    {
        await sender.Send(new DeleteCategoryTypeCommand(id));
        return Results.NoContent();
    }
}
