using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;

namespace BudgetBuddy.Service.ReferenceData.Features.CategoryTypes.UpdateCategoryType;

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

        mapper.Map(request, categoryType);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("CategoryType {CategoryTypeId} updated by user {UserId}", request.Id, UserId);

        return Unit.Value;
    }
}
