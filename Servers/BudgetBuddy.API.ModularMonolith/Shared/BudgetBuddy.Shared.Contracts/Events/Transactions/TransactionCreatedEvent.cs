using NodaTime;

namespace BudgetBuddy.Shared.Contracts.Events.Transactions;

public record TransactionCreatedEvent(
    Guid EventId,
    Guid TransactionId,
    string UserId,
    Guid AccountId,
    Guid? CategoryId,
    decimal Amount,
    string CurrencyCode,
    int TransactionType,  // TransactionType enum value
    LocalDate TransactionDate,
    bool IsTransfer,
    Guid? TransferToAccountId
) : IDomainEvent
{
    public string EventType => nameof(TransactionCreatedEvent);
}
