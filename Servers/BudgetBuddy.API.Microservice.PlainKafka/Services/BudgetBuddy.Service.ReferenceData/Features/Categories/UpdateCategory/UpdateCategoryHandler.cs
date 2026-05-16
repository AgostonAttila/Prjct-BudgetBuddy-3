using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.ReferenceData;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.ReferenceData.Features.Categories.UpdateCategory;

public class UpdateCategoryHandler(
    ReferenceDataDbContext context,
    IMapper mapper,
    ICurrentUserService currentUserService,
    IEventPublisher publisher,
    ILogger<UpdateCategoryHandler> logger) : UserAwareHandler<UpdateCategoryCommand, CategoryResponse>(currentUserService)
{
    public override async Task<CategoryResponse> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating category {CategoryId} for user {UserId}", request.Id, UserId);

        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == UserId, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException(nameof(Category), request.Id);
        }

        category.Name = request.Name;
        category.Icon = request.Icon;
        category.Color = request.Color;

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Category {CategoryId} updated successfully", request.Id);

        await publisher.PublishAsync(new CategoryChangedEvent
        {
            CategoryId = category.Id,
            UserId     = UserId,
            Name       = category.Name,
            Icon       = category.Icon,
            ChangeType = CategoryChangeType.Updated
        }, TopicNames.CategoryChanged, cancellationToken);

        return mapper.Map<CategoryResponse>(category);
    }
}
