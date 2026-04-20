using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.ReferenceData.Features.CategoryTypes.CreateCategoryType;

public class CreateCategoryTypeHandler(
    ReferenceDataDbContext context,
    ICurrentUserService currentUserService,
    IMapper mapper,
    ILogger<CreateCategoryTypeHandler> logger) : UserAwareHandler<CreateCategoryTypeCommand, CreateCategoryTypeResponse>(currentUserService)
{
    public override async Task<CreateCategoryTypeResponse> Handle(
        CreateCategoryTypeCommand request,
        CancellationToken cancellationToken)
    {
        if (!await IsCategoryExist(request, cancellationToken, UserId))
            throw new NotFoundException(nameof(Category), request.CategoryId);

        // Check for duplicate category type name within the category
        // NOTE: Disabled - GlobalExceptionHandler catches DbUpdateException from unique constraint (IX_CategoryTypes_Unique)
        // var exists = await context.CategoryTypes
        //     .AnyAsync(ct => ct.CategoryId == request.CategoryId && ct.Name == request.Name, cancellationToken);
        // if (exists)
        //     throw new ValidationException($"A type with the name '{request.Name}' already exists in this category.");

        var categoryType = mapper.Map<CategoryType>(request);

        context.CategoryTypes.Add(categoryType);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "CategoryType {CategoryTypeId} created for Category {CategoryId} by user {UserId}",
            categoryType.Id,
            request.CategoryId,
            UserId);

        return mapper.Map<CreateCategoryTypeResponse>(categoryType);
    }

    private async Task<bool> IsCategoryExist(CreateCategoryTypeCommand request, CancellationToken cancellationToken,
        string userId)
    {
        // Verify category exists and belongs to user
        return await context.Categories
            .AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId, cancellationToken);

    }
}
