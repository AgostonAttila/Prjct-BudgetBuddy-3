using Mapster;

namespace BudgetBuddy.API.VSA.Features.Categories.UpdateCategory;

public class UpdateCategoryEndpoint : ICarterModule
{
    public record UpdateCategoryRequest(
        string Name,
        string? Icon,
        string? Color
    );

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/categories/{id:guid}", async (
            Guid id,
            UpdateCategoryRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<UpdateCategoryCommand>() with { Id = id };

            var result = await mediator.Send(command, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Update a category")
        .WithDescription("Updates an existing category's name, icon, and color properties.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Categories")
        .WithName("UpdateCategory")
        ;
    }
}


