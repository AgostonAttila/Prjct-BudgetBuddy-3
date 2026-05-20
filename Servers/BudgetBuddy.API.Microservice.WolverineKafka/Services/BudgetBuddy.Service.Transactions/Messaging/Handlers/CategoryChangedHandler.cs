using BudgetBuddy.Service.Transactions.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.ReferenceData;

namespace BudgetBuddy.Service.Transactions.Messaging.Handlers;

[SupportedSchemaVersions("1")]
public class CategoryChangedHandler
{
    protected CategoryChangedHandler() { }

    public static async Task Handle(
        CategoryChangedEvent evt,
        ICategorySnapshotRepository repo,
        ILogger<CategoryChangedHandler> logger,
        CancellationToken ct)
    {
        if (evt.ChangeType == CategoryChangeType.Deleted)
        {
            await repo.DeleteAsync(evt.CategoryId, ct);
            logger.LogInformation("Category {CategoryId} removed from snapshot", evt.CategoryId);
        }
        else
        {
            await repo.UpsertAsync(new CategorySnapshot
            {
                CategoryId = evt.CategoryId,
                UserId     = evt.UserId,
                Name       = evt.Name,
                Icon       = evt.Icon,
                SyncedAt   = DateTime.UtcNow,
            }, ct);
        }
    }
}
