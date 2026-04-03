using BudgetBuddy.API.VSA.Common.Domain.Contracts;
using BudgetBuddy.API.VSA.Common.Infrastructure.Logging;

namespace BudgetBuddy.API.VSA.Common.Domain.Entities;

/// <summary>
/// Security event entity for tracking authentication, authorization, and suspicious activities
/// Used for security monitoring, incident response, and compliance auditing
/// </summary>
public class SecurityEvent : AuditableEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Type of security event (LoginFailure, AccountLockout, TokenRevocation, etc.)
    /// </summary>
    public SecurityEventType EventType { get; set; }

    /// <summary>
    /// Severity level for alerting and filtering
    /// </summary>
    public SecurityEventSeverity Severity { get; set; }

    /// <summary>
    /// User ID if event is associated with a specific user (nullable for anonymous events)
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Email or username for display purposes (masked in logs via PII masking)
    /// </summary>
    [SensitiveData(Strategy = MaskingStrategy.Email)]
    public string? UserIdentifier { get; set; }

    /// <summary>
    /// IP address where event originated
    /// </summary>
    [SensitiveData(Strategy = MaskingStrategy.Partial)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string for device/browser identification
    /// </summary>
    [SensitiveData(Strategy = MaskingStrategy.Partial)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// HTTP endpoint or resource accessed (e.g., "/api/auth/login")
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Detailed event description
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata as JSON (failure reason, admin action details, etc.)
    /// </summary>
    [SensitiveData(Strategy = MaskingStrategy.Partial)]
    public string? Metadata { get; set; }

    /// <summary>
    /// Whether this event has been reviewed by security team
    /// </summary>
    public bool IsReviewed { get; set; }

    /// <summary>
    /// Whether this event triggered an alert
    /// </summary>
    public bool IsAlert { get; set; }
}

public enum SecurityEventType
{
    // Authentication Events
    LoginSuccess = 1,
    LoginFailure = 2,
    AccountLockout = 3,
    PasswordChange = 4,
    PasswordReset = 5,

    // 2FA Events
    TwoFactorEnabled = 10,
    TwoFactorDisabled = 11,
    TwoFactorSuccess = 12,
    TwoFactorFailure = 13,
    RecoveryCodesGenerated = 14,
    RecoveryCodeUsed = 15,

    // Token & Session Events
    TokenRevoked = 20,
    TokenBlacklisted = 21,
    AllTokensRevoked = 22,
    RefreshTokenRotation = 23,

    // Authorization Events
    UnauthorizedAccess = 30,
    RoleChanged = 31,
    PermissionDenied = 32,

    // Rate Limiting & Abuse
    RateLimitExceeded = 40,
    BruteForceDetected = 41,
    SuspiciousActivity = 42,

    // CSRF & Security
    CsrfValidationFailure = 50,
    CsrfTokenMissing = 51,

    // Admin Actions
    AdminActionPerformed = 60,
    UserCreated = 61,
    UserDeleted = 62,
    RoleAssigned = 63,
    RoleRemoved = 64,

    // Data Access
    RowLevelSecurityViolation = 70,
    SensitiveDataAccessed = 71,

    // Other
    SecurityConfigurationChanged = 80,
    AuditLogAccessed = 81
}

public enum SecurityEventSeverity
{
    /// <summary>
    /// Informational event (successful login, normal operations)
    /// </summary>
    Info = 1,

    /// <summary>
    /// Warning - suspicious but not critical (single failed login)
    /// </summary>
    Warning = 2,

    /// <summary>
    /// High - requires attention (multiple failures, lockout)
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical - immediate action required (brute force, unauthorized admin access)
    /// </summary>
    Critical = 4
}
