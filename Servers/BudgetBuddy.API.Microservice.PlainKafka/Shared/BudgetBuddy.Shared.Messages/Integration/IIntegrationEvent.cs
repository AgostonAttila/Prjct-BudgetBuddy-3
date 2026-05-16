namespace BudgetBuddy.Shared.Messages.Integration;

public interface IIntegrationEvent
{
    Guid MessageId { get; init; }
    Guid CorrelationId { get; init; }
    DateTime OccurredAt { get; init; }
    string Version { get; init; }
}
