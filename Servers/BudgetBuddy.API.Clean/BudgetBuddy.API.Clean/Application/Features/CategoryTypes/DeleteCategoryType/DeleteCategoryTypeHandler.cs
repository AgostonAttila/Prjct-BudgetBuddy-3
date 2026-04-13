using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.CategoryTypes.DeleteCategoryType;

public class DeleteCategoryTypeHandler(
    ICategoryTypeRepository categoryTypeRepo,
    IUnitOfWork uow,
    ICurrentUserService currentUserService,
    ILogger<DeleteCategoryTypeHandler> logger) : UserAwareHandler<DeleteCategoryTypeCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteCategoryTypeCommand request,
        CancellationToken cancellationToken)
    {
        var categoryType = await categoryTypeRepo.GetWithCategoryAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CategoryType), request.Id);

        if (categoryType.Category.UserId != UserId)
            throw new UnauthorizedAccessException("You don't have permission to delete this category type");

        categoryTypeRepo.Remove(categoryType);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("CategoryType {CategoryTypeId} deleted by user {UserId}", request.Id, UserId);

        return Unit.Value;
    }
}
