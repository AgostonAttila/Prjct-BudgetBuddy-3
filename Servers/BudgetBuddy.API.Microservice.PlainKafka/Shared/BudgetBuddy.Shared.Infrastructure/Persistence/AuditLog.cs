using BudgetBuddy.Shared.Kernel.Contracts;
using BudgetBuddy.Shared.Kernel.Logging;

namespace BudgetBuddy.Shared.Infrastructure.Persistence;

/// <summary>
/// Audit log entity for tracking all entity changes (Create, Update, Delete).
/// Each service that enables auditing includes this in its own DbContext/schema.
/// </summary>
public class AuditLog : AuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>Entity type name (e.g., "Account", "Transaction", "Budget")</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Primary key of the changed entity (stored as string for flexibility)</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Operation type: Insert, Update, Delete</summary>
    public AuditOperation Operation { get; set; }

    /// <summary>User ID who performed the change (nullable for system operations)</summary>
    public string? UserId { get; set; }

    /// <summary>User identifier for display (email/username - masked in logs)</summary>
    [SensitiveData(Strategy = MaskingStrategy.Email)]
    public string? UserIdentifier { get; set; }

    /// <summary>
    /// Changed properties as JSON: { "PropertyName": { "OldValue": "...", "NewValue": "..." } }
    /// </summary>
    [SensitiveData(Strategy = MaskingStrategy.Partial)]
    public string? Changes { get; set; }

    /// <summary>IP address where change originated</summary>
    [SensitiveData(Strategy = MaskingStrategy.Partial)]
    public string? IpAddress { get; set; }

    /// <summary>User agent string</summary>
    [SensitiveData(Strategy = MaskingStrategy.Partial)]
    public string? UserAgent { get; set; }
}

public enum AuditOperation
{
    Insert = 1,
    Update = 2,
    Delete = 3
}
