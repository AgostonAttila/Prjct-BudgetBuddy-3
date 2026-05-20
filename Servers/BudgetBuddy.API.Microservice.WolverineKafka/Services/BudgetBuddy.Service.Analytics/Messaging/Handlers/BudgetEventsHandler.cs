using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Budgets;

namespace BudgetBuddy.Service.Analytics.Messaging.Handlers;

/// <summary>
/// Maintains the BudgetReadModel.
/// Replaces <c>BudgetEventsConsumer</c> (KafkaMultiTopicConsumerBase).
/// </summary>
[SupportedSchemaVersions("1")]
public static class BudgetEventsHandler
{
    public static async Task Handle(
        BudgetCreatedEvent evt,
        IBudgetReadModelRepository repo,
        CancellationToken ct)
    {
        await repo.UpsertAsync(new BudgetReadModel
        {
            BudgetId     = evt.BudgetId,
            UserId       = evt.UserId,
            CategoryId   = evt.CategoryId,
            Amount       = evt.Amount,
            CurrencyCode = evt.CurrencyCode,
            Year         = evt.Year,
            Month        = evt.Month,
            SyncedAt     = DateTime.UtcNow,
        }, ct);
    }

    public static async Task Handle(
        BudgetUpdatedEvent evt,
        IBudgetReadModelRepository repo,
        CancellationToken ct)
    {
        await repo.UpsertAsync(new BudgetReadModel
        {
            BudgetId     = evt.BudgetId,
            UserId       = evt.UserId,
            CategoryId   = evt.CategoryId,
            Amount       = evt.Amount,
            CurrencyCode = evt.CurrencyCode,
            Year         = evt.Year,
            Month        = evt.Month,
            SyncedAt     = DateTime.UtcNow,
        }, ct);
    }

    public static async Task Handle(
        BudgetDeletedEvent evt,
        IBudgetReadModelRepository repo,
        CancellationToken ct)
    {
        await repo.DeleteAsync(evt.BudgetId, ct);
    }
}
