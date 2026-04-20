using MediatR;

namespace BudgetBuddy.Shared.Contracts.Events;

/// <summary>
/// Marker interface for domain events published via the Outbox pattern.
/// Implement INotification so MediatR can dispatch them to handlers.
/// </summary>
public interface IDomainEvent : INotification
{
    Guid EventId { get; }
    string EventType { get; }
}
