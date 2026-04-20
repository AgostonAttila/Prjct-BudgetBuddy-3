using NodaTime;

namespace BudgetBuddy.Shared.Contracts.Events.Transactions;

public record TransactionUpdatedEvent(
    Guid EventId,
    Guid TransactionId,
    string UserId,
    Guid AccountId,
    Guid? CategoryId,
    decimal Amount,
    string CurrencyCode,
    int TransactionType,
    LocalDate TransactionDate
) : IDomainEvent
{
    public string EventType => nameof(TransactionUpdatedEvent);
}
