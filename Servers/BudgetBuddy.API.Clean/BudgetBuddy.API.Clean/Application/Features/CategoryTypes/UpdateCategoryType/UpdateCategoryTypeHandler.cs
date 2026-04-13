using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.CategoryTypes.UpdateCategoryType;

public class UpdateCategoryTypeHandler(
    ICategoryTypeRepository categoryTypeRepo,
    IUnitOfWork uow,
    ICurrentUserService currentUserService,
    IMapper mapper,
    ILogger<UpdateCategoryTypeHandler> logger) : UserAwareHandler<UpdateCategoryTypeCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        UpdateCategoryTypeCommand request,
        CancellationToken cancellationToken)
    {
        var categoryType = await categoryTypeRepo.GetWithCategoryAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(CategoryType), request.Id);

        if (categoryType.Category.UserId != UserId)
            throw new UnauthorizedAccessException("You don't have permission to update this category type");

        mapper.Map(request, categoryType);

        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("CategoryType {CategoryTypeId} updated by user {UserId}", request.Id, UserId);

        return Unit.Value;
    }
}
