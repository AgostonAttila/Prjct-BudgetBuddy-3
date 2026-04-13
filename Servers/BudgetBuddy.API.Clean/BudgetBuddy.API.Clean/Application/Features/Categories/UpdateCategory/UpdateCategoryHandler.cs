using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Categories.UpdateCategory;

public class UpdateCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ILogger<UpdateCategoryHandler> logger) : UserAwareHandler<UpdateCategoryCommand, CategoryResponse>(currentUserService)
{
    public override async Task<CategoryResponse> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating category {CategoryId} for user {UserId}", request.Id, UserId);

        var category = await categoryRepo.GetByIdAsync(request.Id, UserId, cancellationToken);

        if (category == null)
            throw new NotFoundException(nameof(Category), request.Id);

        category.Name = request.Name;
        category.Icon = request.Icon;
        category.Color = request.Color;

        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Category {CategoryId} updated successfully", request.Id);

        return mapper.Map<CategoryResponse>(category);
    }
}
