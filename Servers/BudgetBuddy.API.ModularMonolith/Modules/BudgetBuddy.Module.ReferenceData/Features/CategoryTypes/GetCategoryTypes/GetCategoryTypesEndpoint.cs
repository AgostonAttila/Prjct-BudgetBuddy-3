namespace BudgetBuddy.Module.ReferenceData.Features.CategoryTypes.GetCategoryTypes;

public class GetCategoryTypesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/category-types", async (
            ISender sender,
            Guid? categoryId,
            int page = 1,
            int pageSize = 10) =>
        {
            var query = new GetCategoryTypesQuery(categoryId, page, pageSize);
            var result = await sender.Send(query);

            return Results.Ok(result);
        })
        .WithSummary("Get all category types")
        .WithDescription("Retrieves all category types with optional filtering by category and pagination support.")
        .RequireAuthorization()
        .CacheOutput("category-types")
        .WithTags("Category Types")
        .WithName("GetCategoryTypes")
        .Produces<GetCategoryTypesResponse>(200)
        ;
    }
}
