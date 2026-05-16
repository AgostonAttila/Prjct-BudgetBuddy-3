using System.Text.Json;
using BudgetBuddy.Service.ReferenceData.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Messages;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.ReferenceData.Messaging.Consumers;

/// <summary>
/// Consumes the compacted accounts.changelog topic to maintain a local AccountSnapshot read model.
/// Null payload = tombstone (account deleted).
/// </summary>
public sealed class AccountChangelogConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<AccountChangelogConsumer> logger)
    : KafkaMultiTopicConsumerBase(
        settings,
        [TopicNames.AccountsChangelog],
        ConsumerGroups.ReferenceDataAccounts,
        scopeFactory,
        logger)
{
    protected override string? DlqTopic => TopicNames.AccountsDlq;

    protected override async Task ProcessAsync(
        string topic, string? payload, string messageKey, long offset,
        IServiceProvider services, CancellationToken ct)
    {
        var repo = services.GetRequiredService<IAccountSnapshotRepository>();

        if (payload is null)
        {
            // Tombstone — account deleted
            await repo.DeleteAsync(Guid.Parse(messageKey), ct);
            Logger.LogInformation("Account {AccountId} removed from ReferenceData snapshot (tombstone)", messageKey);
            return;
        }

        var msg = JsonSerializer.Deserialize<AccountSnapshotMessage>(payload)!;

        // Out-of-order protection: skip if we already have a newer version
        var existing = await repo.FindAsync(msg.AccountId, ct);
        if (existing is not null && existing.Version >= offset)
        {
            return;
        }

        await repo.UpsertAsync(new AccountSnapshot
        {
            AccountId = msg.AccountId,
            UserId    = msg.UserId,
            Name      = msg.Name,
            Currency  = msg.Currency,
            IsActive  = msg.IsActive,
            Version   = offset,
            SyncedAt  = DateTime.UtcNow,
        }, ct);
    }
}
