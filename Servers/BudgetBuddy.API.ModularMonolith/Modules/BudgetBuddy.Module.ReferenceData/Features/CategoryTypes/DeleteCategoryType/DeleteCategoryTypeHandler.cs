using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Module.ReferenceData.Features.CategoryTypes.DeleteCategoryType;

public class DeleteCategoryTypeHandler(
    ReferenceDataDbContext context,
    ICurrentUserService currentUserService,
    ILogger<DeleteCategoryTypeHandler> logger) : UserAwareHandler<DeleteCategoryTypeCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteCategoryTypeCommand request,
        CancellationToken cancellationToken)
    {
        var categoryType = await context.CategoryTypes
            .Include(ct => ct.Category)
            .FirstOrDefaultAsync(ct => ct.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CategoryType), request.Id);

        // Verify ownership through category
        if (categoryType.Category.UserId != UserId)
        {
            throw new UnauthorizedAccessException("You don't have permission to delete this category type");
        }

        context.CategoryTypes.Remove(categoryType);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("CategoryType {CategoryTypeId} deleted by user {UserId}", request.Id, UserId);

        return Unit.Value;
    }
}
