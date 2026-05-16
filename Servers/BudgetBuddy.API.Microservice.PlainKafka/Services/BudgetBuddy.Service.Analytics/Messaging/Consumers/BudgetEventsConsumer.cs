using System.Text.Json;
using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Budgets;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.Analytics.Messaging.Consumers;

/// <summary>
/// Consumes budget CRUD events to maintain a local BudgetReadModel.
/// </summary>
public sealed class BudgetEventsConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<BudgetEventsConsumer> logger)
    : KafkaMultiTopicConsumerBase(
        settings,
        [TopicNames.BudgetsCreated, TopicNames.BudgetsUpdated, TopicNames.BudgetsDeleted],
        ConsumerGroups.AnalyticsBudgets,
        scopeFactory,
        logger)
{
    protected override string? DlqTopic => TopicNames.BudgetsDlq;

    protected override async Task ProcessAsync(
        string topic, string? payload, string messageKey, long offset,
        IServiceProvider services, CancellationToken ct)
    {
        var repo = services.GetRequiredService<IBudgetReadModelRepository>();

        if (topic == TopicNames.BudgetsDeleted)
        {
            var evt = JsonSerializer.Deserialize<BudgetDeletedEvent>(payload!)!;
            await repo.DeleteAsync(evt.BudgetId, ct);
        }
        else
        {
            using var doc = JsonDocument.Parse(payload!);
            var root = doc.RootElement;
            var model = new BudgetReadModel
            {
                BudgetId     = root.GetProperty("BudgetId").GetGuid(),
                UserId       = root.GetProperty("UserId").GetString()!,
                CategoryId   = root.GetProperty("CategoryId").GetGuid(),
                Amount       = root.GetProperty("Amount").GetDecimal(),
                CurrencyCode = root.GetProperty("CurrencyCode").GetString()!,
                Year         = root.GetProperty("Year").GetInt32(),
                Month        = root.GetProperty("Month").GetInt32(),
                SyncedAt     = DateTime.UtcNow,
            };
            await repo.UpsertAsync(model, ct);
        }
    }
}
