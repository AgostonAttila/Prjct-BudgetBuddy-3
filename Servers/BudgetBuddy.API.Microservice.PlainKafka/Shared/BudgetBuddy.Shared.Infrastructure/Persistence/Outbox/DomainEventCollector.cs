using BudgetBuddy.Shared.Messages.Topics;

namespace BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;

/// <summary>
/// Scoped service. Handlers call Collect() during the request,
/// then the OutboxInterceptor drains it during SaveChanges.
/// </summary>
public sealed class DomainEventCollector : IDomainEventCollector
{
    private readonly List<IIntegrationEvent> _events = [];

    public void Collect(IIntegrationEvent integrationEvent) => _events.Add(integrationEvent);

    public IReadOnlyList<IIntegrationEvent> GetAndClear()
    {
        var copy = _events.ToList();
        _events.Clear();
        return copy;
    }
}
