using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace BudgetBuddy.API.Middlewares;

public class RequestTimeLoggingMiddleware(ILogger<RequestTimeLoggingMiddleware> logger, IOptions<RequestTimeLoggingOptions> options) : IMiddleware
{
    private readonly int _thresholdSeconds = options.Value.RequestTimeThresholdSeconds;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var stopWatch = Stopwatch.StartNew();
        await next.Invoke(context);
        stopWatch.Stop();

        if (stopWatch.Elapsed.TotalSeconds > _thresholdSeconds)
        {
            logger.LogInformation("[PERFORMANCE] The request [{Verb}] at {Path} took {Time} ms",
                context.Request.Method,
                context.Request.Path,
                stopWatch.ElapsedMilliseconds);
        }
    }
}

public class RequestTimeLoggingOptions
{
    public int RequestTimeThresholdSeconds { get; set; } = 4;
}