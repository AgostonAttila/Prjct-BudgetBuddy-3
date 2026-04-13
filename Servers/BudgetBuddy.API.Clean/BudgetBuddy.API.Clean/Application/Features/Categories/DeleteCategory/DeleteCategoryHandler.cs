using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Categories.DeleteCategory;

public class DeleteCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWork uow,
    ICurrentUserService currentUserService,
    ILogger<DeleteCategoryHandler> logger) : UserAwareHandler<DeleteCategoryCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting category {CategoryId} for user {UserId}", request.Id, UserId);

        var category = await categoryRepo.GetByIdAsync(request.Id, UserId, cancellationToken);

        if (category == null)
            throw new NotFoundException(nameof(Category), request.Id);

        categoryRepo.Remove(category);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Category {CategoryId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
