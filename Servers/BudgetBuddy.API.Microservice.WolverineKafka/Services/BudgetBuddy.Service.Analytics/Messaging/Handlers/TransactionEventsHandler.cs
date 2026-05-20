using BudgetBuddy.Service.Analytics.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Transactions;

namespace BudgetBuddy.Service.Analytics.Messaging.Handlers;

/// <summary>
/// Maintains the TransactionReadModel and invalidates analytics cache.
/// Replaces <c>TransactionEventsConsumer</c> (KafkaMultiTopicConsumerBase).
/// </summary>
[SupportedSchemaVersions("1")]
public static class TransactionEventsHandler
{
    public static async Task Handle(
        TransactionCreatedEvent evt,
        ITransactionReadModelRepository repo,
        IUserCacheInvalidator cache,
        CancellationToken ct)
    {
        await cache.InvalidateAsync(evt.UserId, ct);
        await repo.UpsertAsync(new TransactionReadModel
        {
            TransactionId   = evt.TransactionId,
            UserId          = evt.UserId,
            AccountId       = evt.AccountId,
            CategoryId      = evt.CategoryId,
            TransactionType = evt.TransactionType,
            Amount          = evt.Amount,
            CurrencyCode    = evt.CurrencyCode,
            TransactionDate = evt.TransactionDate,
            SyncedAt        = DateTime.UtcNow,
        }, ct);
    }

    public static async Task Handle(
        TransactionUpdatedEvent evt,
        ITransactionReadModelRepository repo,
        IUserCacheInvalidator cache,
        CancellationToken ct)
    {
        await cache.InvalidateAsync(evt.UserId, ct);
        await repo.UpsertAsync(new TransactionReadModel
        {
            TransactionId   = evt.TransactionId,
            UserId          = evt.UserId,
            AccountId       = evt.AccountId,
            CategoryId      = evt.CategoryId,
            TransactionType = evt.TransactionType,
            Amount          = evt.Amount,
            CurrencyCode    = evt.CurrencyCode,
            TransactionDate = evt.TransactionDate,
            SyncedAt        = DateTime.UtcNow,
        }, ct);
    }

    public static async Task Handle(
        TransactionDeletedEvent evt,
        ITransactionReadModelRepository repo,
        IUserCacheInvalidator cache,
        CancellationToken ct)
    {
        await cache.InvalidateAsync(evt.UserId, ct);
        await repo.DeleteAsync(evt.TransactionId, ct);
    }
}
