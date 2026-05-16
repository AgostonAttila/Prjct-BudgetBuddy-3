using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;

public interface IDomainEventCollector
{
    void Collect(IIntegrationEvent integrationEvent);
    IReadOnlyList<IIntegrationEvent> GetAndClear();
}
