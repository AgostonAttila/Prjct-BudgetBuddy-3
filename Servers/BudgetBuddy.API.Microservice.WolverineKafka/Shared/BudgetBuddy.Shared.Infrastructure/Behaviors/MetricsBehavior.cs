using System.Diagnostics;

namespace BudgetBuddy.Shared.Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior that records handler duration and invocation counts
/// to Prometheus via <see cref="HandlerMetrics"/> (finding #12).
///
/// Registered after <see cref="LoggingBehavior{TRequest,TResponse}"/> so that
/// timing includes validation but not the outer logging wrapper.
/// </summary>
public class MetricsBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var handlerName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        var success = false;

        try
        {
            var result = await next();
            success = true;
            return result;
        }
        finally
        {
            sw.Stop();
            var tags = new[]
            {
                new KeyValuePair<string, object?>("handler_name", handlerName),
                new KeyValuePair<string, object?>("success", success),
            };
            HandlerMetrics.HandlerDuration.Record(sw.Elapsed.TotalSeconds, tags);
            HandlerMetrics.HandlerInvocations.Add(1, tags);
        }
    }
}
