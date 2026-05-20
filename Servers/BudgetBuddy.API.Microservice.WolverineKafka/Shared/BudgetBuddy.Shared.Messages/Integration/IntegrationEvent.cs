namespace BudgetBuddy.Shared.Messages.Integration;

/// <summary>
/// Abstract base record for all integration events.
/// Eliminates boilerplate — derived records only declare domain-specific properties.
/// </summary>
public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public Guid CorrelationId { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string Version { get; init; } = "1.0";
}
