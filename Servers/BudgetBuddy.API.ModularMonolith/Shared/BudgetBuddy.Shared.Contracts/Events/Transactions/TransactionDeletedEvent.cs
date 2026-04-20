namespace BudgetBuddy.Shared.Contracts.Events.Transactions;

public record TransactionDeletedEvent(
    Guid EventId,
    Guid TransactionId,
    string UserId
) : IDomainEvent
{
    public string EventType => nameof(TransactionDeletedEvent);
}
