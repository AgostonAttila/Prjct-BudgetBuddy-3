using BudgetBuddy.Application.Features.Categories.CreateCategory;
using BudgetBuddy.Application.Features.Categories.DeleteCategory;
using BudgetBuddy.Application.Features.Categories.GetCategories;
using BudgetBuddy.Application.Features.Categories.UpdateCategory;
using Mapster;

namespace BudgetBuddy.API.Endpoints;

public record CreateCategoryRequest(string Name, string? Icon, string? Color);
public record UpdateCategoryRequest(string Name, string? Icon, string? Color);

public class CategoryModule : CarterModule
{
    public CategoryModule() : base("/api/categories")
    {
        WithTags("Categories");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", GetCategories)
            .WithSummary("Get all categories")
            .WithDescription("Retrieves all transaction categories for the authenticated user.")
            .WithStandardRateLimit()
            .CacheOutput("categories")
            .WithName("GetCategories");

        app.MapPost("", CreateCategory)
            .WithSummary("Create a new category")
            .WithDescription("Creates a new transaction category for the authenticated user with optional icon and color customization.")
            .WithStandardRateLimit()
            .WithName("CreateCategory");

        app.MapPut("{id:guid}", UpdateCategory)
            .WithSummary("Update a category")
            .WithDescription("Updates an existing category's name, icon, and color properties.")
            .WithStandardRateLimit()
            .WithName("UpdateCategory");

        app.MapDelete("{id:guid}", DeleteCategory)
            .WithSummary("Delete a category")
            .WithDescription("Permanently deletes a specific category by its unique identifier. Warning: This may affect related transactions and budgets.")
            .WithStandardRateLimit()
            .WithName("DeleteCategory");
    }

    private static async Task<IResult> GetCategories(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCategoriesQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateCategory(CreateCategoryRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateCategoryCommand>();
        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/categories/{result.Id}", result);
    }

    private static async Task<IResult> UpdateCategory(Guid id, UpdateCategoryRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateCategoryCommand>() with { Id = id };
        var result = await mediator.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteCategory(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        return Results.NoContent();
    }
}
