using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.CategoryTypes.CreateCategoryType;

public class CreateCategoryTypeHandler(
    ICategoryTypeRepository categoryTypeRepo,
    ICategoryRepository categoryRepo,
    IUnitOfWork uow,
    ICurrentUserService currentUserService,
    IMapper mapper,
    ILogger<CreateCategoryTypeHandler> logger) : UserAwareHandler<CreateCategoryTypeCommand, CreateCategoryTypeResponse>(currentUserService)
{
    public override async Task<CreateCategoryTypeResponse> Handle(
        CreateCategoryTypeCommand request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepo.GetByIdAsync(request.CategoryId, UserId, cancellationToken);

        if (category == null)
            throw new NotFoundException(nameof(Category), request.CategoryId);

        var categoryType = mapper.Map<CategoryType>(request);

        categoryTypeRepo.Add(categoryType);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "CategoryType {CategoryTypeId} created for Category {CategoryId} by user {UserId}",
            categoryType.Id,
            request.CategoryId,
            UserId);

        return mapper.Map<CreateCategoryTypeResponse>(categoryType);
    }
}
