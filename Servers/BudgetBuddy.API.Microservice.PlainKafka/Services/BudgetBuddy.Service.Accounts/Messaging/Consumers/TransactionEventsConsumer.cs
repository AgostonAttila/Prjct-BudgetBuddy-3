using System.Text.Json;
using BudgetBuddy.Service.Accounts.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Transactions;
using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Service.Accounts.Messaging.Consumers;

/// <summary>
/// Consumes transaction events to maintain the AccountTransactionTotals read model.
/// Used by AccountBalanceService to calculate balances without cross-service calls.
/// Idempotent via LastProcessedMessageId: duplicate deliveries are skipped in the repository.
/// </summary>
public sealed class TransactionEventsConsumer(
    KafkaSettings settings,
    IServiceScopeFactory scopeFactory,
    ILogger<TransactionEventsConsumer> logger)
    : KafkaMultiTopicConsumerBase(
        settings,
        [TopicNames.TransactionsCreated, TopicNames.TransactionsUpdated, TopicNames.TransactionsDeleted],
        ConsumerGroups.AccountsTransactions,
        scopeFactory,
        logger)
{
    protected override string? DlqTopic => TopicNames.TransactionsDlq;

    protected override async Task ProcessAsync(
        string topic, string? payload, string messageKey, long offset,
        IServiceProvider services, CancellationToken ct)
    {
        var repo = services.GetRequiredService<IAccountTransactionTotalsRepository>();

        switch (topic)
        {
            case TopicNames.TransactionsCreated:
            {
                var evt = JsonSerializer.Deserialize<TransactionCreatedEvent>(payload!)!;
                var (income, expense) = ToDeltas(evt.TransactionType, evt.Amount);
                await repo.ApplyDeltaAsync(evt.AccountId, income, expense, countDelta: 1, evt.MessageId, ct);
                break;
            }
            case TopicNames.TransactionsUpdated:
            {
                var evt = JsonSerializer.Deserialize<TransactionUpdatedEvent>(payload!)!;
                var amountDelta = evt.Amount - evt.OldAmount;
                if (amountDelta != 0)
                {
                    var (income, expense) = ToDeltas(evt.TransactionType, amountDelta);
                    await repo.ApplyDeltaAsync(evt.AccountId, income, expense, countDelta: 0, evt.MessageId, ct);
                }
                break;
            }
            case TopicNames.TransactionsDeleted:
            {
                var evt = JsonSerializer.Deserialize<TransactionDeletedEvent>(payload!)!;
                var (income, expense) = ToDeltas(evt.TransactionType, -evt.Amount);
                await repo.ApplyDeltaAsync(evt.AccountId, income, expense, countDelta: -1, evt.MessageId, ct);
                break;
            }
        }
    }

    private static (decimal income, decimal expense) ToDeltas(TransactionType type, decimal amount)
        => type == TransactionType.Income ? (amount, 0) : (0, amount);
}
