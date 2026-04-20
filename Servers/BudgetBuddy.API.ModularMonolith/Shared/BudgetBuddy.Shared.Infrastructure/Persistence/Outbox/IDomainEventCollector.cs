using BudgetBuddy.Shared.Contracts.Events;

namespace BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;

public interface IDomainEventCollector
{
    void Collect(IDomainEvent domainEvent);
    IReadOnlyList<IDomainEvent> GetAndClear();
}
