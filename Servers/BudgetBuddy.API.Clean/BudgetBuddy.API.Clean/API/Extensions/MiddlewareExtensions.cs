using BudgetBuddy.API.Middlewares;
using Serilog;

namespace BudgetBuddy.API.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseMiddlewarePipeline(this WebApplication app)
    {
        // Response Compression kell legyen első
        app.UseResponseCompression();

        // Custom middlewares
        app.UseSecurityHeaders(SecurityExtensions.GetSecurityHeaders(app.Environment.IsDevelopment()));
        app.UseMiddleware<CorrelationIdMiddleware>();

        // Exception handling
        app.UseExceptionHandler();

        // Logging
        app.UseSerilogRequestLogging();

        // Output Cache
        app.UseOutputCache();

        // ⚠️ KNOWN SECURITY GAP: Built-in /auth/refresh lacks token reuse detection (OWASP vulnerability).
        // DisableInsecureRefreshEndpointMiddleware exists but CANNOT be activated until the full custom
        // auth flow (login, register, logout) is implemented — the built-in BearerTokenStore and the
        // custom DB-backed RefreshToken store are incompatible.
        // See: API/Middlewares/DisableInsecureRefreshEndpointMiddleware.cs for the ready solution.
        // TODO: Implement custom login/register/logout, then add: app.UseDisableInsecureRefreshEndpoint();

        return app;
    }
}