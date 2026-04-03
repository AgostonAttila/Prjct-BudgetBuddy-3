using System.Security.Claims;
using System.Text.Json;

namespace BudgetBuddy.API.VSA.Common.Infrastructure;

/// <summary>
/// Service for logging and monitoring security events
/// </summary>
public class SecurityEventService(AppDbContext context, ILogger<SecurityEventService> logger) : ISecurityEventService
{
  

    public async Task LogEventAsync(
        SecurityEventType eventType,
        SecurityEventSeverity severity,
        string message,
        string? userId = null,
        string? userIdentifier = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? endpoint = null,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var securityEvent = new SecurityEvent
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Severity = severity,
                Message = message,
                UserId = userId,
                UserIdentifier = userIdentifier,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Endpoint = endpoint,
                Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                IsReviewed = false,
                IsAlert = severity >= SecurityEventSeverity.High // Auto-alert for High/Critical
            };

            await context.SecurityEvents.AddAsync(securityEvent, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            // Log to Serilog for immediate visibility
            var logLevel = severity switch
            {
                SecurityEventSeverity.Critical => LogLevel.Critical,
                SecurityEventSeverity.High => LogLevel.Error,
                SecurityEventSeverity.Warning => LogLevel.Warning,
                _ => LogLevel.Information
            };

            logger.Log(logLevel,
                "Security Event: {EventType} | {Severity} | User: {UserId} | IP: {IpAddress} | {Message}",
                eventType, severity, userId ?? "Anonymous", ipAddress ?? "Unknown", message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
            // Don't throw - security logging failures shouldn't break the app
        }
    }

    public async Task LogEventFromContextAsync(
        HttpContext httpContext,
        SecurityEventType eventType,
        SecurityEventSeverity severity,
        string message,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userIdentifier = httpContext.User.FindFirstValue(ClaimTypes.Email)
                           ?? httpContext.User.FindFirstValue(ClaimTypes.Name);
        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        var endpoint = $"{httpContext.Request.Method} {httpContext.Request.Path}";

        await LogEventAsync(
            eventType,
            severity,
            message,
            userId,
            userIdentifier,
            ipAddress,
            userAgent,
            endpoint,
            metadata,
            cancellationToken);
    }

    public async Task<bool> DetectBruteForceAsync(
        string? userId,
        string? ipAddress,
        TimeSpan timeWindow,
        int threshold,
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = Instant.FromDateTimeUtc(DateTime.UtcNow.Subtract(timeWindow));

        var query = context.SecurityEvents
            .Where(e => e.CreatedAt >= cutoffTime)
            .Where(e => e.EventType == SecurityEventType.LoginFailure ||
                       e.EventType == SecurityEventType.TwoFactorFailure);

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(e => e.UserId == userId);
        }

        if (!string.IsNullOrEmpty(ipAddress))
        {
            query = query.Where(e => e.IpAddress == ipAddress);
        }

        var failureCount = await query.CountAsync(cancellationToken);

        if (failureCount >= threshold)
        {
            // Log brute force detection
            await LogEventAsync(
                SecurityEventType.BruteForceDetected,
                SecurityEventSeverity.Critical,
                $"Brute force detected: {failureCount} failures in {timeWindow.TotalMinutes} minutes",
                userId,
                null,
                ipAddress,
                null,
                null,
                new { FailureCount = failureCount, TimeWindowMinutes = timeWindow.TotalMinutes },
                cancellationToken);

            return true;
        }

        return false;
    }

    public async Task<List<SecurityEvent>> GetRecentEventsAsync(
        int count = 50,
        SecurityEventSeverity? minSeverity = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.SecurityEvents
            .OrderByDescending(e => e.CreatedAt)
            .Take(count);

        if (minSeverity.HasValue)
        {
            query = (IOrderedQueryable<SecurityEvent>)query.Where(e => e.Severity >= minSeverity.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<SecurityEvent>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        return await context.SecurityEvents
            .AsNoTracking() 
            .Where(e => e.IsAlert && !e.IsReviewed)
            .Where(e => e.Severity >= SecurityEventSeverity.High)
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReviewedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var securityEvent = await context.SecurityEvents.FindAsync(new object[] { eventId }, cancellationToken);

        if (securityEvent != null)
        {
            securityEvent.IsReviewed = true;
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Security event {EventId} marked as reviewed", eventId);
        }
    }
}
