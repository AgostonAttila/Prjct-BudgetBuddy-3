using BudgetBuddy.Shared.Messages.Integration;

namespace BudgetBuddy.Shared.Infrastructure.Messaging;

public interface IEventPublisher
{
    /// <summary>Publish an event. Uses CorrelationId as the Kafka partition key by default.</summary>
    Task PublishAsync<T>(T @event, string topic, CancellationToken ct = default)
        where T : IIntegrationEvent;

    /// <summary>
    /// Publish an event with an explicit partition key (K-03).
    /// Use the entity's own ID (e.g. AccountId) to guarantee ordering per entity.
    /// </summary>
    Task PublishAsync<T>(T @event, string topic, string partitionKey, CancellationToken ct = default)
        where T : IIntegrationEvent;

    /// <summary>Non-generic overload for dynamic dispatch (e.g. outbox processor).</summary>
    Task PublishAsync(IIntegrationEvent @event, Type eventType, string topic, CancellationToken ct = default);

    /// <summary>Non-generic overload with explicit partition key (K-03).</summary>
    Task PublishAsync(IIntegrationEvent @event, Type eventType, string topic, string partitionKey, CancellationToken ct = default);

    /// <summary>Publish to a compacted topic (full state, keyed by entityId).</summary>
    Task PublishStateAsync<T>(string topic, string key, T state, CancellationToken ct = default);

    /// <summary>Publish tombstone to compacted topic (signals deletion).</summary>
    Task PublishTombstoneAsync(string topic, string key, CancellationToken ct = default);
}
