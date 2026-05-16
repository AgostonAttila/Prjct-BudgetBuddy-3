using System.Text.Json;
using BudgetBuddy.Service.Transactions.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Messages;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.Transactions.Messaging.Consumers;

/// <summary>
/// Consumes the compacted accounts.changelog topic to maintain a local AccountSnapshot read model.
/// On fresh deploy: AutoOffsetReset.Earliest replays all messages → full state.
/// Null payload = tombstone (account deleted).
/// </summary>
public sealed class AccountChangelogConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<AccountChangelogConsumer> logger)
    : KafkaMultiTopicConsumerBase(
        settings,
        [TopicNames.AccountsChangelog],
        ConsumerGroups.TransactionsAccounts,
        scopeFactory,
        logger)
{
    protected override string? DlqTopic => TopicNames.AccountsDlq;

    protected override async Task ProcessAsync(
        string topic, string? payload, string messageKey, long offset,
        IServiceProvider services, CancellationToken ct)
    {
        var repo    = services.GetRequiredService<IAccountSnapshotRepository>();
        var service = services.GetRequiredService<IAccountSnapshotService>();

        if (payload is null)
        {
            // Tombstone — account deleted
            var accountId = Guid.Parse(messageKey);
            await repo.DeleteAsync(accountId, ct);
            await service.InvalidateAsync(accountId, ct);
            Logger.LogInformation("Account {AccountId} removed from snapshot (tombstone)", accountId);
            return;
        }

        var msg = JsonSerializer.Deserialize<AccountSnapshotMessage>(payload)!;

        // Out-of-order protection: skip if we already have a newer version
        var existing = await repo.FindAsync(msg.AccountId, ct);
        if (existing is not null && existing.Version >= offset)
        {
            return;
        }

        var snapshot = new AccountSnapshot
        {
            AccountId = msg.AccountId,
            UserId    = msg.UserId,
            Name      = msg.Name,
            Currency  = msg.Currency,
            IsActive  = msg.IsActive,
            Version   = offset,
            SyncedAt  = DateTime.UtcNow,
        };

        await repo.UpsertAsync(snapshot, ct);
        await service.PopulateAsync(snapshot, ct);
    }
}
