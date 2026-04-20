using BudgetBuddy.Shared.Contracts.Events;

namespace BudgetBuddy.Shared.Infrastructure.Persistence.Outbox;

/// <summary>
/// Scoped service. Handlers call Collect() during the request,
/// then the OutboxInterceptor drains it during SaveChanges.
/// </summary>
public sealed class DomainEventCollector : IDomainEventCollector
{
    private readonly List<IDomainEvent> _events = [];

    public void Collect(IDomainEvent domainEvent) => _events.Add(domainEvent);

    public IReadOnlyList<IDomainEvent> GetAndClear()
    {
        var copy = _events.ToList();
        _events.Clear();
        return copy;
    }
}
