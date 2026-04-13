using BudgetBuddy.Application.Common.Handlers;
using BudgetBuddy.Application.Common.Repositories;

namespace BudgetBuddy.Application.Features.Categories.CreateCategory;

public class CreateCategoryHandler(
    ICategoryRepository categoryRepo,
    IUnitOfWork uow,
    IMapper mapper,
    ICurrentUserService currentUserService,
    ILogger<CreateCategoryHandler> logger) : UserAwareHandler<CreateCategoryCommand, CreateCategoryResponse>(currentUserService)
{
    public override async Task<CreateCategoryResponse> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating category {CategoryName} for user {UserId}", request.Name, UserId);

        var category = mapper.Map<Category>(request);
        category.UserId = UserId;

        categoryRepo.Add(category);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Category {CategoryId} created successfully", category.Id);

        return mapper.Map<CreateCategoryResponse>(category);
    }
}
