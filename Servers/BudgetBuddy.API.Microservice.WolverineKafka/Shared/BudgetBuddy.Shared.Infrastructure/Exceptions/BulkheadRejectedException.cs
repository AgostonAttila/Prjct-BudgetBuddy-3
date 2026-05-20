namespace BudgetBuddy.Shared.Infrastructure.Exceptions;

/// <summary>
/// Thrown by <see cref="BudgetBuddy.Shared.Infrastructure.Behaviors.BulkheadBehavior{TRequest,TResponse}"/>
/// when the concurrent request limit for a handler is exceeded.
/// Maps to HTTP 503 Service Unavailable.
/// </summary>
public sealed class BulkheadRejectedException(string requestType)
    : Exception($"Bulkhead limit reached for '{requestType}'. Server is too busy — retry shortly.");
