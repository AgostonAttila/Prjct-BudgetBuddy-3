namespace BudgetBuddy.Shared.Infrastructure.Middlewares;

/// <summary>
/// Middleware that validates an internal API key on selected routes.
/// Apply via endpoint group: group.RequireInternalApiKey()
/// Config: ServiceAuth:InternalApiKey
/// Item 11: Service-to-service API Key authentication
/// </summary>
public sealed class InternalApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<InternalApiKeyMiddleware> logger)
{
    private const string HeaderName      = "X-Internal-Api-Key";
    private const string ConfigKey       = "ServiceAuth:InternalApiKey";
    private const string InternalPathPrefix = "/internal";

    public async Task InvokeAsync(HttpContext context)
    {
        // Only enforce on /internal/** paths
        if (!context.Request.Path.StartsWithSegments(InternalPathPrefix))
        {
            await next(context);
            return;
        }

        var expectedKey = configuration[ConfigKey];

        if (string.IsNullOrEmpty(expectedKey))
        {
            // No key configured — deny to be fail-closed
            logger.LogError("InternalApiKey not configured (ServiceAuth:InternalApiKey). Blocking /internal request.");
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new { error = "Service misconfigured" });
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var providedKey) ||
            !string.Equals(providedKey, expectedKey, StringComparison.Ordinal))
        {
            logger.LogWarning("Rejected /internal request — missing or invalid {Header} from {RemoteIp}",
                HeaderName, context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        await next(context);
    }
}

/// <summary>
/// Extension methods for registering internal API key protection on endpoint groups.
/// </summary>
public static class InternalApiKeyExtensions
{
    /// <summary>
    /// Adds the <see cref="InternalApiKeyMiddleware"/> to the pipeline.
    /// Call this in Program.cs before UseAuthentication if you want it to run for all /internal routes.
    /// </summary>
    public static IApplicationBuilder UseInternalApiKey(this IApplicationBuilder app)
        => app.UseMiddleware<InternalApiKeyMiddleware>();
}
