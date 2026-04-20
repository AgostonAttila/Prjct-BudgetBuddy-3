using System.Security.Claims;

namespace BudgetBuddy.Module.Auth.Infrastructure.Middlewares;

/// <summary>
/// Middleware that automatically logs security events for authentication and authorization endpoints
/// Tracks login, logout, registration, 2FA operations, and failed attempts
/// </summary>
public class SecurityEventMiddleware(
    RequestDelegate next,
    ILogger<SecurityEventMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext context,
        ISecurityEventService securityEventService)
    {
        var path = context.Request.Path.ToString().ToLowerInvariant();
        var method = context.Request.Method.ToUpperInvariant();

        if (!IsSecurityEndpoint(path))
        {
            await next(context);
            return;
        }

        try
        {
            await next(context);
            await LogSecurityEventAsync(context, securityEventService, path, method);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SecurityEventMiddleware for {Path}", path);
            throw;
        }
    }

    private static bool IsSecurityEndpoint(string path)
    {
        return path.StartsWith("/auth/") ||
               path.StartsWith("/api/twofactor/") ||
               path.StartsWith("/api/authentication/");
    }

    private async Task LogSecurityEventAsync(
        HttpContext context,
        ISecurityEventService securityEventService,
        string path,
        string method)
    {
        var statusCode = context.Response.StatusCode;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userIdentifier = context.User.FindFirstValue(ClaimTypes.Email)
                           ?? context.User.FindFirstValue(ClaimTypes.Name);
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var endpoint = $"{method} {context.Request.Path}";

        try
        {
            if (path.Contains("/login"))
            {
                await HandleLoginEvent(securityEventService, statusCode, userId, userIdentifier, ipAddress, userAgent, endpoint);
            }
            else if (path.Contains("/logout"))
            {
                await HandleLogoutEvent(securityEventService, statusCode, userId, userIdentifier, ipAddress, userAgent, endpoint);
            }
            else if (path.Contains("/register"))
            {
                await HandleRegistrationEvent(securityEventService, statusCode, userId, userIdentifier, ipAddress, userAgent, endpoint);
            }
            else if (path.Contains("/twofactor/enable") || path.Contains("/enabletwofactor"))
            {
                await HandleTwoFactorEnableEvent(securityEventService, statusCode, userId, userIdentifier, ipAddress, userAgent, endpoint);
            }
            else if (path.Contains("/twofactor/disable") || path.Contains("/disabletwofactor"))
            {
                await HandleTwoFactorDisableEvent(securityEventService, statusCode, userId, userIdentifier, ipAddress, userAgent, endpoint);
            }
            else if (path.Contains("/twofactor/verify") || path.Contains("/verifytwofactor"))
            {
                await HandleTwoFactorVerifyEvent(securityEventService, statusCode, userId, userIdentifier, ipAddress, userAgent, endpoint);
            }
            else if (path.Contains("/refresh"))
            {
                await HandleTokenRefreshEvent(securityEventService, statusCode, userId, userIdentifier, ipAddress, userAgent, endpoint);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to log security event for {Path}", path);
            // Don't throw - security logging failures shouldn't break requests
        }
    }

    private static async Task HandleLoginEvent(
        ISecurityEventService securityEventService,
        int statusCode, string? userId, string? userIdentifier,
        string? ipAddress, string? userAgent, string endpoint)
    {
        if (statusCode == 200)
        {
            await securityEventService.LogEventAsync(
                SecurityEventType.LoginSuccess, SecurityEventSeverity.Info,
                "User logged in successfully",
                userId, userIdentifier, ipAddress, userAgent, endpoint);
        }
        else if (statusCode == 401 || statusCode == 400)
        {
            await securityEventService.LogEventAsync(
                SecurityEventType.LoginFailure, SecurityEventSeverity.Warning,
                "Login attempt failed - invalid credentials",
                null, userIdentifier, ipAddress, userAgent, endpoint);

            await securityEventService.DetectBruteForceAsync(
                userId, ipAddress, TimeSpan.FromMinutes(15), threshold: 5);
        }
    }

    private static async Task HandleLogoutEvent(
        ISecurityEventService securityEventService,
        int statusCode, string? userId, string? userIdentifier,
        string? ipAddress, string? userAgent, string endpoint)
    {
        if (statusCode == 200)
        {
            await securityEventService.LogEventAsync(
                SecurityEventType.TokenRevoked, SecurityEventSeverity.Info,
                "User logged out successfully",
                userId, userIdentifier, ipAddress, userAgent, endpoint);
        }
    }

    private static async Task HandleRegistrationEvent(
        ISecurityEventService securityEventService,
        int statusCode, string? userId, string? userIdentifier,
        string? ipAddress, string? userAgent, string endpoint)
    {
        if (statusCode == 200)
        {
            await securityEventService.LogEventAsync(
                SecurityEventType.UserCreated, SecurityEventSeverity.Info,
                "New user registered successfully",
                userId, userIdentifier, ipAddress, userAgent, endpoint);
        }
    }

    private static async Task HandleTwoFactorEnableEvent(
        ISecurityEventService securityEventService,
        int statusCode, string? userId, string? userIdentifier,
        string? ipAddress, string? userAgent, string endpoint)
    {
        if (statusCode == 200)
        {
            await securityEventService.LogEventAsync(
                SecurityEventType.TwoFactorEnabled, SecurityEventSeverity.Info,
                "Two-factor authentication enabled",
                userId, userIdentifier, ipAddress, userAgent, endpoint);
        }
    }

    private static async Task HandleTwoFactorDisableEvent(
        ISecurityEventService securityEventService,
        int statusCode, string? userId, string? userIdentifier,
        string? ipAddress, string? userAgent, string endpoint)
    {
        if (statusCode == 200)
        {
            await securityEventService.LogEventAsync(
                SecurityEventType.TwoFactorDisabled, SecurityEventSeverity.Warning,
                "Two-factor authentication disabled",
                userId, userIdentifier, ipAddress, userAgent, endpoint);
        }
    }

    private static async Task HandleTwoFactorVerifyEvent(
        ISecurityEventService securityEventService,
        int statusCode, string? userId, string? userIdentifier,
        string? ipAddress, string? userAgent, string endpoint)
    {
        if (statusCode == 200)
        {
            await securityEventService.LogEventAsync(
                SecurityEventType.TwoFactorSuccess, SecurityEventSeverity.Info,
                "Two-factor authentication verification succeeded",
                userId, userIdentifier, ipAddress, userAgent, endpoint);
        }
        else if (statusCode == 401 || statusCode == 400)
        {
            await securityEventService.LogEventAsync(
                SecurityEventType.TwoFactorFailure, SecurityEventSeverity.Warning,
                "Two-factor authentication verification failed",
                userId, userIdentifier, ipAddress, userAgent, endpoint);

            await securityEventService.DetectBruteForceAsync(
                userId, ipAddress, TimeSpan.FromMinutes(15), threshold: 5);
        }
    }

    private static async Task HandleTokenRefreshEvent(
        ISecurityEventService securityEventService,
        int statusCode, string? userId, string? userIdentifier,
        string? ipAddress, string? userAgent, string endpoint)
    {
        if (statusCode == 200)
        {
            await securityEventService.LogEventAsync(
                SecurityEventType.RefreshTokenRotation, SecurityEventSeverity.Info,
                "Access token refreshed successfully",
                userId, userIdentifier, ipAddress, userAgent, endpoint);
        }
    }
}
