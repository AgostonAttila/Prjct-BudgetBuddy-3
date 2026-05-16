using BudgetBuddy.Shared.Infrastructure.Middlewares;
using Serilog;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseMiddlewarePipeline(this WebApplication app)
    {
        // Response compression must come first
        app.UseResponseCompression();

        // Custom middlewares
        app.UseSecurityHeaders(SecurityExtensions.GetSecurityHeaders(app.Environment.IsDevelopment()));
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<InternalApiKeyMiddleware>();   // Item 11: blocks /internal/** without X-Internal-Api-Key

        // Exception handling
        app.UseExceptionHandler();

        // Logging
        app.UseSerilogRequestLogging();

        // Output Cache
        app.UseOutputCache();

        return app;
    }
}