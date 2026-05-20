using BudgetBuddy.Service.Investments.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Accounts;

namespace BudgetBuddy.Service.Investments.Messaging.Handlers;

/// <summary>
/// Maintains the local AccountSnapshot read model from Wolverine account events.
/// Replaces AccountChangelogConsumer (KafkaMultiTopicConsumerBase, compacted topic).
/// Balance is set from AccountCreatedEvent.InitialBalance and preserved on updates.
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
            Balance   = evt.InitialBalance,
            SyncedAt  = DateTime.UtcNow,
        }, ct);

        logger.LogInformation("Account {AccountId} snapshot created in Investments", evt.AccountId);
    }

    public static async Task Handle(
        AccountUpdatedEvent evt,
        IAccountSnapshotRepository repo,
        ILogger<AccountSnapshotHandler> logger,
        CancellationToken ct)
    {
        // Preserve existing balance — AccountUpdatedEvent does not carry balance
        var existing = await repo.FindAsync(evt.AccountId, ct);

        await repo.UpsertAsync(new AccountSnapshot
        {
            AccountId = evt.AccountId,
            UserId    = evt.UserId,
            Name      = evt.Name,
            Currency  = evt.Currency,
            IsActive  = evt.IsActive,
            Balance   = existing?.Balance ?? 0,
            SyncedAt  = DateTime.UtcNow,
        }, ct);

        logger.LogInformation("Account {AccountId} snapshot updated in Investments", evt.AccountId);
    }

    public static async Task Handle(
        AccountDeletedEvent evt,
        IAccountSnapshotRepository repo,
        ILogger<AccountSnapshotHandler> logger,
        CancellationToken ct)
    {
        await repo.DeleteAsync(evt.AccountId, ct);

        logger.LogInformation("Account {AccountId} snapshot deleted from Investments", evt.AccountId);
    }
}
