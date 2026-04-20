using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.ReferenceData.Features.CategoryTypes.UpdateCategoryType;

public class UpdateCategoryTypeHandler(
    ReferenceDataDbContext context,
    ICurrentUserService currentUserService,
    IMapper mapper,
    ILogger<UpdateCategoryTypeHandler> logger) : UserAwareHandler<UpdateCategoryTypeCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        UpdateCategoryTypeCommand request,
        CancellationToken cancellationToken)
    {
        var categoryType = await context.CategoryTypes
            .Include(ct => ct.Category)
            .FirstOrDefaultAsync(ct => ct.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CategoryType), request.Id);

        // Verify ownership through category
        if (categoryType.Category.UserId != UserId)
        {
            throw new UnauthorizedAccessException("You don't have permission to update this category type");
        }

        // Check for duplicate category type name within the category (excluding current type)
        // NOTE: Disabled - GlobalExceptionHandler catches DbUpdateException from unique constraint (IX_CategoryTypes_Unique)
        // var exists = await context.CategoryTypes
        //     .AnyAsync(ct =>
        //         ct.CategoryId == categoryType.CategoryId &&
        //         ct.Name == request.Name &&
        //         ct.Id != request.Id,
        //         cancellationToken);
        // if (exists)
        //     throw new ValidationException($"A type with the name '{request.Name}' already exists in this category.");

        mapper.Map(request, categoryType);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("CategoryType {CategoryTypeId} updated by user {UserId}", request.Id, UserId);

        return Unit.Value;
    }
}
