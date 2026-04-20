using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.ReferenceData.Features.Categories.CreateCategory;

public class CreateCategoryHandler(
    ReferenceDataDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ILogger<CreateCategoryHandler> logger) : UserAwareHandler<CreateCategoryCommand, CreateCategoryResponse>(currentUserService)
{


    public override async Task<CreateCategoryResponse> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating category {CategoryName} for user {UserId}", request.Name, UserId);

        // Check for duplicate category name
        // NOTE: Disabled - GlobalExceptionHandler catches DbUpdateException from unique constraint (IX_Categories_Unique)
        // var exists = await context.Categories
        //     .AnyAsync(c => c.UserId == userId && c.Name == request.Name, cancellationToken);
        // if (exists)
        //     throw new ValidationException($"A category with the name '{request.Name}' already exists.");

        var category = mapper.Map<Category>(request);
        category.UserId = UserId;

        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Category {CategoryId} created successfully", category.Id);

        return mapper.Map<CreateCategoryResponse>(category);

    }
}
