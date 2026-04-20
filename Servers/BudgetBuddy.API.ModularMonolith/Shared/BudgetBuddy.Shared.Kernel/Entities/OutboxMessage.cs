using NodaTime;

namespace BudgetBuddy.Shared.Kernel.Entities;

/// <summary>
/// Persisted domain event waiting to be published via MediatR.
/// Each module that publishes events has its own OutboxMessages table in its schema.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Fully-qualified event type name (used for deserialization).</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>JSON-serialized event payload.</summary>
    public string Payload { get; set; } = string.Empty;

    public Instant CreatedAt { get; set; }

    /// <summary>Null until successfully published.</summary>
    public Instant? ProcessedAt { get; set; }

    /// <summary>Last error message if processing failed.</summary>
    public string? Error { get; set; }

    /// <summary>How many times publishing was attempted.</summary>
    public int RetryCount { get; set; }
}
