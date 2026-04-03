using BudgetBuddy.API.VSA.Common.Infrastructure;
using BudgetBuddy.API.VSA.Common.Shared.Handlers;

namespace BudgetBuddy.API.VSA.Features.Categories.UpdateCategory;

public class UpdateCategoryHandler(
    AppDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ILogger<UpdateCategoryHandler> logger) : UserAwareHandler<UpdateCategoryCommand, CategoryResponse>(currentUserService)
{
    public override async Task<CategoryResponse> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating category {CategoryId} for user {UserId}", request.Id, UserId);

        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == UserId, cancellationToken);

        if (category == null)
            throw new NotFoundException(nameof(Category), request.Id);

        // Check for duplicate category name (excluding current category)
        // NOTE: Disabled - GlobalExceptionHandler catches DbUpdateException from unique constraint (IX_Categories_Unique)
        // var exists = await context.Categories
        //     .AnyAsync(c =>
        //         c.UserId == userId &&
        //         c.Name == request.Name &&
        //         c.Id != request.Id,
        //         cancellationToken);
        // if (exists)
        //     throw new ValidationException($"A category with the name '{request.Name}' already exists.");

        category.Name = request.Name;
        category.Icon = request.Icon;
        category.Color = request.Color;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Category {CategoryId} updated successfully", request.Id);

        return mapper.Map<CategoryResponse>(category);
    }
}
