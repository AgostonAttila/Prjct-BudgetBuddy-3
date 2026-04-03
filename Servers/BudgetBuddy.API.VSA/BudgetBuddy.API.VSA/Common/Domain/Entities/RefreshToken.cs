using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetBuddy.API.VSA.Common.Domain.Entities;

/// <summary>
/// Refresh token entity for token rotation and reuse detection
/// Implements OWASP best practices for refresh token security
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The refresh token value (HASHED in database via HashedStringConverter)
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Plain text token for returning to client (NOT PERSISTED TO DATABASE)
    /// This is set by TokenService and used only for the response
    /// </summary>
    [NotMapped]
    public string? PlainToken { get; set; }

    /// <summary>
    /// User who owns this token (ASP.NET Identity uses string for Id)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to User
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Token expiration time (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Token creation time (UTC)
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Token that replaced this token (for rotation tracking)
    /// CRITICAL: Used for reuse detection!
    /// If this is set and token is used again → SECURITY BREACH
    /// </summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// When the token was revoked (UTC)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Reason for revocation (e.g., "Replaced by new token", "Security breach", "User logout")
    /// </summary>
    public string? RevokedReason { get; set; }

    /// <summary>
    /// IP address from which the token was created
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// IP address from which the token was revoked
    /// </summary>
    public string? RevokedByIp { get; set; }

    // Computed properties

    /// <summary>
    /// Token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Token is revoked
    /// </summary>
    public bool IsRevoked => RevokedAt != null;

    /// <summary>
    /// Token is active (not expired and not revoked)
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired;
}
