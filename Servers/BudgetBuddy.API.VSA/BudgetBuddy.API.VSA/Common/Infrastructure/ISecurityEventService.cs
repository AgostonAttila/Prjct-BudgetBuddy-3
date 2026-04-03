namespace BudgetBuddy.API.VSA.Common.Infrastructure;

/// <summary>
/// Service for logging and tracking security events
/// </summary>
public interface ISecurityEventService
{
    /// <summary>
    /// Log a security event
    /// </summary>
    Task LogEventAsync(
        SecurityEventType eventType,
        SecurityEventSeverity severity,
        string message,
        string? userId = null,
        string? userIdentifier = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? endpoint = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log a security event from HttpContext
    /// </summary>
    Task LogEventFromContextAsync(
        HttpContext httpContext,
        SecurityEventType eventType,
        SecurityEventSeverity severity,
        string message,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check for brute force attacks (multiple failures from same IP/user)
    /// </summary>
    Task<bool> DetectBruteForceAsync(
        string? userId,
        string? ipAddress,
        TimeSpan timeWindow,
        int threshold,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent security events for dashboard
    /// </summary>
    Task<List<SecurityEvent>> GetRecentEventsAsync(
        int count = 50,
        SecurityEventSeverity? minSeverity = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active alerts (unreviewed critical/high severity events)
    /// </summary>
    Task<List<SecurityEvent>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark event as reviewed
    /// </summary>
    Task MarkAsReviewedAsync(Guid eventId, CancellationToken cancellationToken = default);
}
