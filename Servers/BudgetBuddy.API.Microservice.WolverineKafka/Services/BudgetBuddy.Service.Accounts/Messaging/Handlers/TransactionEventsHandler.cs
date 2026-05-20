using BudgetBuddy.Service.Accounts.ReadModels;
using BudgetBuddy.Shared.Infrastructure.Messaging;
using BudgetBuddy.Shared.Messages.Events.Transactions;

namespace BudgetBuddy.Service.Accounts.Messaging.Handlers;

/// <summary>
/// Maintains the AccountTransactionTotals read model.
/// Replaces <c>TransactionEventsConsumer</c> (KafkaMultiTopicConsumerBase).
/// Idempotency is handled inside <see cref="IAccountTransactionTotalsRepository.ApplyDeltaAsync"/>
/// via LastProcessedMessageId.
/// </summary>
[SupportedSchemaVersions("1")]
public static class TransactionEventsHandler
{
    public static async Task Handle(
        TransactionCreatedEvent evt,
        IAccountTransactionTotalsRepository repo,
        CancellationToken ct)
    {
        var (income, expense) = ToDeltas(evt.TransactionType, evt.Amount);
        await repo.ApplyDeltaAsync(evt.AccountId, income, expense, countDelta: 1, evt.MessageId, ct);
    }

    public static async Task Handle(
        TransactionUpdatedEvent evt,
        IAccountTransactionTotalsRepository repo,
        CancellationToken ct)
    {
        var amountDelta = evt.Amount - evt.OldAmount;
        if (amountDelta != 0)
        {
            var (income, expense) = ToDeltas(evt.TransactionType, amountDelta);
            await repo.ApplyDeltaAsync(evt.AccountId, income, expense, countDelta: 0, evt.MessageId, ct);
        }
    }

    public static async Task Handle(
        TransactionDeletedEvent evt,
        IAccountTransactionTotalsRepository repo,
        CancellationToken ct)
    {
        var (income, expense) = ToDeltas(evt.TransactionType, -evt.Amount);
        await repo.ApplyDeltaAsync(evt.AccountId, income, expense, countDelta: -1, evt.MessageId, ct);
    }

    private static (decimal income, decimal expense) ToDeltas(TransactionType type, decimal amount)
        => type == TransactionType.Income ? (amount, 0) : (0, amount);
}
