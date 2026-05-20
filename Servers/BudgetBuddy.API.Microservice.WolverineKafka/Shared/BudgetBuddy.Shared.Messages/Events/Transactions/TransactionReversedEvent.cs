using System.Text.Json.Serialization;
using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Messages.Events.Transactions;

/// <summary>
/// Domain event raised when the saga coordinator creates a compensation (reversal) transaction.
/// Published to <see cref="BudgetBuddy.Shared.Messages.Topics.TopicNames.TransactionsReversed"/>
/// via the Transactional Outbox after a failed AccountBalanceReservedEvent.
/// </summary>
public sealed record TransactionReversedEvent : IntegrationEvent
{
    [JsonRequired] public Guid   OriginalTransactionId { get; init; }
    [JsonRequired] public Guid   ReversalTransactionId { get; init; }
    [JsonRequired] public string Reason                { get; init; } = string.Empty;
}
