using BudgetBuddy.API.VSA.Common.Middlewares;
using Serilog;

namespace BudgetBuddy.API.VSA.Common.Extensions;

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

        return app;
    }
}