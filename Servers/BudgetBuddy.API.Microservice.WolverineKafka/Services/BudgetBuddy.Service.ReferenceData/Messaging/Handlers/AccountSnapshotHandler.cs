using BudgetBuddy.Service.ReferenceData.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Accounts;

namespace BudgetBuddy.Service.ReferenceData.Messaging.Handlers;

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
        ILogger<AccountSnapshotHandler> logger,
        CancellationToken ct)
    {
        await repo.UpsertAsync(new AccountSnapshot
        {
            AccountId = evt.AccountId,
            UserId    = evt.UserId,
            Name      = evt.Name,
            Currency  = evt.Currency,
            IsActive  = true,
            SyncedAt  = DateTime.UtcNow,
        }, ct);

        logger.LogInformation("Account {AccountId} snapshot created in ReferenceData", evt.AccountId);
    }

    public static async Task Handle(
        AccountUpdatedEvent evt,
        IAccountSnapshotRepository repo,
        ILogger<AccountSnapshotHandler> logger,
        CancellationToken ct)
    {
        await repo.UpsertAsync(new AccountSnapshot
        {
            AccountId = evt.AccountId,
            UserId    = evt.UserId,
            Name      = evt.Name,
            Currency  = evt.Currency,
            IsActive  = evt.IsActive,
            SyncedAt  = DateTime.UtcNow,
        }, ct);

        logger.LogInformation("Account {AccountId} snapshot updated in ReferenceData", evt.AccountId);
    }

    public static async Task Handle(
        AccountDeletedEvent evt,
        IAccountSnapshotRepository repo,
        ILogger<AccountSnapshotHandler> logger,
        CancellationToken ct)
    {
        await repo.DeleteAsync(evt.AccountId, ct);

        logger.LogInformation("Account {AccountId} snapshot deleted from ReferenceData", evt.AccountId);
    }
}
