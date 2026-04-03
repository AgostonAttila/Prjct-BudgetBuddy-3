using Mapster;

namespace BudgetBuddy.API.VSA.Features.Categories.CreateCategory;

public class CreateCategoryEndpoint : ICarterModule
{
    public record CreateCategoryRequest(
        string Name,
        string? Icon,
        string? Color
    );

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/categories", async (
            CreateCategoryRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<CreateCategoryCommand>();

            var result = await mediator.Send(command, cancellationToken);

            return Results.Created($"/api/categories/{result.Id}", result);
        })
        .WithSummary("Create a new category")
        .WithDescription("Creates a new transaction category for the authenticated user with optional icon and color customization.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Categories")
        .WithName("CreateCategory");
    }
}


