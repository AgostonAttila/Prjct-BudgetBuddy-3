using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.ReferenceData;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.ReferenceData.Features.Categories.CreateCategory;

public class CreateCategoryHandler(
    ReferenceDataDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IEventPublisher publisher,
    ILogger<CreateCategoryHandler> logger) : UserAwareHandler<CreateCategoryCommand, CreateCategoryResponse>(currentUserService)
{


    public override async Task<CreateCategoryResponse> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating category {CategoryName} for user {UserId}", request.Name, UserId);

        var category = mapper.Map<Category>(request);
        category.UserId = UserId;

        context.Categories.Add(category);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Category {CategoryId} created successfully", category.Id);

        await publisher.PublishAsync(new CategoryChangedEvent
        {
            CategoryId = category.Id,
            UserId     = UserId,
            Name       = category.Name,
            Icon       = category.Icon,
            ChangeType = CategoryChangeType.Created
        }, TopicNames.CategoryChanged, cancellationToken);

        return mapper.Map<CreateCategoryResponse>(category);

    }
}
