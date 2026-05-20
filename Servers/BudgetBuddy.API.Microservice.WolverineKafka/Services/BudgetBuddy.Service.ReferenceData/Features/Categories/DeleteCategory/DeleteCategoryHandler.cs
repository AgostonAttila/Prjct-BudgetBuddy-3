using BudgetBuddy.Shared.Infrastructure;
using BudgetBuddy.Shared.Infrastructure.Handlers;
using BudgetBuddy.Shared.Messages.Events.ReferenceData;
using Wolverine.EntityFrameworkCore;

namespace BudgetBuddy.Service.ReferenceData.Features.Categories.DeleteCategory;

public class DeleteCategoryHandler(
    ReferenceDataDbContext context,
    ICurrentUserService currentUserService,
    IDbContextOutbox outbox,
    ILogger<DeleteCategoryHandler> logger) : UserAwareHandler<DeleteCategoryCommand, Unit>(currentUserService)
{
    public override async Task<Unit> Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting category {CategoryId} for user {UserId}", request.Id, UserId);

        var category = await context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.UserId == UserId, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException(nameof(Category), request.Id);
        }

        context.Categories.Remove(category);

        outbox.Enroll(context);
        await outbox.SendAsync(new CategoryChangedEvent
        {
            CategoryId = category.Id,
            UserId     = UserId,
            Name       = category.Name,
            Icon       = category.Icon,
            ChangeType = CategoryChangeType.Deleted
        });
        await outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);

        logger.LogInformation("Category {CategoryId} deleted successfully", request.Id);

        return Unit.Value;
    }
}
