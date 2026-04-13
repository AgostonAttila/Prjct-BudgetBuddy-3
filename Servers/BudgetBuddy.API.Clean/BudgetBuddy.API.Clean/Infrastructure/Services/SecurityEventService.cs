using BudgetBuddy.Application.Common.Repositories;
using System.Security.Claims;
using System.Text.Json;

namespace BudgetBuddy.Infrastructure.Services;

public class SecurityEventService(
    ISecurityEventRepository securityEventRepo,
    ILogger<SecurityEventService> logger) : ISecurityEventService
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
                IsAlert = severity >= SecurityEventSeverity.High
            };

            await securityEventRepo.AddAsync(securityEvent, cancellationToken);

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
            eventType, severity, message,
            userId, userIdentifier, ipAddress, userAgent, endpoint,
            metadata, cancellationToken);
    }

    public async Task<bool> DetectBruteForceAsync(
        string? userId,
        string? ipAddress,
        TimeSpan timeWindow,
        int threshold,
        CancellationToken cancellationToken = default)
    {
        var cutoffTime = Instant.FromDateTimeUtc(DateTime.UtcNow.Subtract(timeWindow));

        var failureCount = await securityEventRepo.CountFailuresAsync(
            userId, ipAddress, cutoffTime, cancellationToken);

        if (failureCount >= threshold)
        {
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
        return await securityEventRepo.GetRecentAsync(count, minSeverity, cancellationToken);
    }

    public async Task<List<SecurityEvent>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        return await securityEventRepo.GetActiveAlertsAsync(cancellationToken);
    }

    public async Task MarkAsReviewedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        await securityEventRepo.MarkAsReviewedAsync(eventId, cancellationToken);
        logger.LogInformation("Security event {EventId} marked as reviewed", eventId);
    }
}
