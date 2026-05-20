using BudgetBuddy.Service.Transactions.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Accounts;

namespace BudgetBuddy.Service.Transactions.Messaging.Handlers;

/// <summary>
/// Maintains the local AccountSnapshot read model from Wolverine account events.
/// Replaces AccountChangelogConsumer (KafkaMultiTopicConsumerBase, compacted topic).
/// </summary>
[SupportedSchemaVersions("1")]
public class AccountSnapshotHandler
{
    protected AccountSnapshotHandler() { }

    public static async Task Handle(
        AccountCreatedEvent evt,
        IAccountSnapshotRepository repo,
        IAccountSnapshotService cache,
        ILogger<AccountSnapshotHandler> logger,
        CancellationToken ct)
    {
        var snapshot = new AccountSnapshot
        {
            AccountId = evt.AccountId,
            UserId    = evt.UserId,
            Name      = evt.Name,
            Currency  = evt.Currency,
            IsActive  = true,
            SyncedAt  = DateTime.UtcNow,
        };

        await repo.UpsertAsync(snapshot, ct);
        await cache.PopulateAsync(snapshot, ct);

        logger.LogInformation("Account {AccountId} snapshot created", evt.AccountId);
    }

    public static async Task Handle(
        AccountUpdatedEvent evt,
        IAccountSnapshotRepository repo,
        IAccountSnapshotService cache,
        ILogger<AccountSnapshotHandler> logger,
        CancellationToken ct)
    {
        var snapshot = new AccountSnapshot
        {
            AccountId = evt.AccountId,
            UserId    = evt.UserId,
            Name      = evt.Name,
            Currency  = evt.Currency,
            IsActive  = evt.IsActive,
            SyncedAt  = DateTime.UtcNow,
        };

        await repo.UpsertAsync(snapshot, ct);
        await cache.PopulateAsync(snapshot, ct);

        logger.LogInformation("Account {AccountId} snapshot updated", evt.AccountId);
    }

    public static async Task Handle(
        AccountDeletedEvent evt,
        IAccountSnapshotRepository repo,
        IAccountSnapshotService cache,
        ILogger<AccountSnapshotHandler> logger,
        CancellationToken ct)
    {
        await repo.DeleteAsync(evt.AccountId, ct);
        await cache.InvalidateAsync(evt.AccountId, ct);

        logger.LogInformation("Account {AccountId} snapshot deleted", evt.AccountId);
    }
}
