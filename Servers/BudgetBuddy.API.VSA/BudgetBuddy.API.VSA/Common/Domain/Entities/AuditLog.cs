using BudgetBuddy.API.VSA.Common.Domain.Contracts;
using BudgetBuddy.API.VSA.Common.Infrastructure.Logging;

namespace BudgetBuddy.API.VSA.Common.Domain.Entities;

/// <summary>
/// Audit log entity for tracking all entity changes (Create, Update, Delete)
/// Used for compliance, debugging, and security investigations
/// </summary>
public class AuditLog : AuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Entity type name (e.g., "Account", "Transaction", "Budget")
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Primary key of the changed entity (stored as string for flexibility)
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Operation type: Insert, Update, Delete
    /// </summary>
    public AuditOperation Operation { get; set; }

    /// <summary>
    /// User ID who performed the change (nullable for system operations)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// User identifier for display (email/username - masked in logs)
    /// </summary>
    [SensitiveData(Strategy = MaskingStrategy.Email)]
    public string? UserIdentifier { get; set; }

    /// <summary>
    /// Changed properties as JSON: { "PropertyName": { "OldValue": "...", "NewValue": "..." } }
    /// Example: { "Amount": { "OldValue": "100", "NewValue": "150" } }
    /// </summary>
    [SensitiveData(Strategy = MaskingStrategy.Partial)]
    public string? Changes { get; set; }

    /// <summary>
    /// IP address where change originated
    /// </summary>
    [SensitiveData(Strategy = MaskingStrategy.Partial)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    [SensitiveData(Strategy = MaskingStrategy.Partial)]
    public string? UserAgent { get; set; }
}

public enum AuditOperation
{
    /// <summary>
    /// Entity was created (INSERT)
    /// </summary>
    Insert = 1,

    /// <summary>
    /// Entity was modified (UPDATE)
    /// </summary>
    Update = 2,

    /// <summary>
    /// Entity was deleted (DELETE)
    /// </summary>
    Delete = 3
}
