using System.Collections.Concurrent;
using BudgetBuddy.Shared.Infrastructure.Exceptions;

namespace BudgetBuddy.Shared.Infrastructure.Behaviors;

/// <summary>
/// A-16: Per-request-type bulkhead that caps the number of concurrent handler invocations.
/// Uses a static <see cref="SemaphoreSlim"/> per TRequest so each command/query type
/// gets its own concurrency slot — one slow handler cannot starve unrelated ones.
///
/// If the semaphore is taken (all slots occupied), the request is rejected immediately
/// with <see cref="BulkheadRejectedException"/> → HTTP 503, rather than queuing indefinitely.
/// The limit is read from <c>Bulkhead:MaxConcurrency</c> (default 20).
/// </summary>
public sealed class BulkheadBehavior<TRequest, TResponse>(
    IConfiguration configuration,
    ILogger<BulkheadBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConcurrentDictionary<Type, SemaphoreSlim> _semaphores = new();

    private SemaphoreSlim GetSemaphore()
    {
        var limit = configuration.GetValue("Bulkhead:MaxConcurrency", 20);
        return _semaphores.GetOrAdd(typeof(TRequest), _ => new SemaphoreSlim(limit, limit));
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var semaphore = GetSemaphore();

        if (!await semaphore.WaitAsync(TimeSpan.Zero, cancellationToken))
        {
            logger.LogWarning(
                "Bulkhead rejected {RequestType} — {Available}/{Total} slots occupied",
                typeof(TRequest).Name,
                0,
                configuration.GetValue("Bulkhead:MaxConcurrency", 20));

            throw new BulkheadRejectedException(typeof(TRequest).Name);
        }

        try
        {
            return await next();
        }
        finally
        {
            semaphore.Release();
        }
    }
}
