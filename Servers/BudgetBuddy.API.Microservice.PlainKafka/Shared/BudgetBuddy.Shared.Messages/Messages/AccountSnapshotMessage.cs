namespace BudgetBuddy.Shared.Messages.Messages;

/// <summary>
/// Full account state published to the compacted changelog topic.
/// Used by other services (e.g. Transactions) to maintain a local account read model.
/// Null value on the Kafka message = tombstone (account deleted).
/// </summary>
public record AccountSnapshotMessage
{
    public Guid AccountId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public decimal Balance { get; init; }
}
