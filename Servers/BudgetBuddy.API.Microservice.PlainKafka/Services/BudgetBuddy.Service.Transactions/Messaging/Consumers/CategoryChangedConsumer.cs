using BudgetBuddy.Service.Transactions.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.ReferenceData;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.Transactions.Messaging.Consumers;

public sealed class CategoryChangedConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<CategoryChangedConsumer> logger)
    : KafkaConsumerBase<CategoryChangedEvent>(
        settings,
        TopicNames.CategoryChanged,
        ConsumerGroups.TransactionsReferenceData,
        scopeFactory,
        logger)
{
    protected override string? DlqTopic => TopicNames.ReferenceDataDlq;

    protected override async Task ProcessAsync(
        CategoryChangedEvent evt, IServiceProvider services, CancellationToken ct)
    {
        var repo = services.GetRequiredService<ICategorySnapshotRepository>();

        if (evt.ChangeType == CategoryChangeType.Deleted)
        {
            await repo.DeleteAsync(evt.CategoryId, ct);
            Logger.LogInformation("Category {CategoryId} removed from snapshot", evt.CategoryId);
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
