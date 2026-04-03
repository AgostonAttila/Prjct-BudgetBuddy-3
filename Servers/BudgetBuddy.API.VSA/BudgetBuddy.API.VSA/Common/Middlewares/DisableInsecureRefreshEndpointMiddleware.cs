namespace BudgetBuddy.API.VSA.Common.Middlewares;

// ============================================================================
// ⚠️ NOTE: This middleware is NOT currently used
// ============================================================================
// This would be the CORRECT security solution, but requires a FULL custom auth implementation.
//
// PROBLEM:
// - Built-in /auth/login saves refresh tokens in memory (BearerTokenStore)
// - Custom /auth/token/refresh looks for tokens in the DATABASE
// - These two systems are INCOMPATIBLE!
//
// SOLUTION (to use this middleware):
// 1. Implement custom /auth/login endpoint that saves refresh tokens to DB
// 2. Implement custom /auth/register endpoint
// 3. Implement custom /auth/logout endpoint
// 4. Then enable this middleware to block the insecure built-in /auth/refresh
//
// WHY THIS IS BETTER:
// - Token rotation (new refresh token on every use)
// - Token reuse detection (security breach alerts)
// - Token family revocation (all tokens revoked if breach detected)
// - Proper audit trail in database
//
// CURRENT STATE:
// - Using built-in /auth/refresh (NO reuse detection - security risk!)
// - Custom /auth/token/refresh exists but is INCOMPATIBLE with built-in login
// ============================================================================

/// <summary>
/// Middleware that blocks the built-in /auth/refresh endpoint which lacks token reuse detection.
/// Redirects clients to use /auth/token/refresh instead (secure alternative).
/// </summary>
public class DisableInsecureRefreshEndpointMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DisableInsecureRefreshEndpointMiddleware> _logger;

    public DisableInsecureRefreshEndpointMiddleware(
        RequestDelegate next,
        ILogger<DisableInsecureRefreshEndpointMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        // Block the built-in /auth/refresh endpoint
        if (path.Equals("/auth/refresh", StringComparison.OrdinalIgnoreCase) &&
            context.Request.Method == HttpMethods.Post)
        {
            _logger.LogWarning(
                "Blocked request to insecure /auth/refresh endpoint. " +
                "IP: {IpAddress}, User-Agent: {UserAgent}. " +
                "Client should use /auth/token/refresh instead.",
                context.Connection.RemoteIpAddress,
                context.Request.Headers.UserAgent.ToString());

            context.Response.StatusCode = StatusCodes.Status410Gone;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                error = "endpoint_disabled",
                message = "This endpoint has been disabled for security reasons. Use /auth/token/refresh instead.",
                reason = "Built-in /auth/refresh lacks token reuse detection (OWASP security vulnerability)",
                disabledEndpoint = "/auth/refresh",
                replacementEndpoint = "/auth/token/refresh",
                securityFeatures = new[]
                {
                    "Token rotation (new refresh token on every use)",
                    "Token reuse detection (alerts on suspicious activity)",
                    "Token family revocation (all tokens revoked if breach detected)"
                }
            });

            return; // Short-circuit the pipeline
        }

        // Continue to next middleware
        await _next(context);
    }
}

/// <summary>
/// Extension method for adding the DisableInsecureRefreshEndpointMiddleware to the pipeline
/// </summary>
public static class DisableInsecureRefreshEndpointMiddlewareExtensions
{
    public static IApplicationBuilder UseDisableInsecureRefreshEndpoint(this IApplicationBuilder app)
    {
        return app.UseMiddleware<DisableInsecureRefreshEndpointMiddleware>();
    }
}
