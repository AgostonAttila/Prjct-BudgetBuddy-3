using BudgetBuddy.Shared.Infrastructure.Filters;

namespace BudgetBuddy.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for adding idempotency protection to endpoints
/// </summary>
public static class IdempotencyEndpointExtensions
{
    /// <summary>
    /// Enables idempotency for this endpoint using Idempotency-Key header.
    /// Use for CREATE operations that should not be duplicated (transactions, transfers, payments).
    /// </summary>
    /// <remarks>
    /// Clients should send a unique Idempotency-Key header (e.g., a GUID) with each request.
    /// If the same key is used within 24 hours, the cached response is returned.
    ///
    /// Example:
    /// <code>
    /// POST /api/transactions
    /// Headers:
    ///   Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000
    ///   Authorization: Bearer {token}
    /// </code>
    /// </remarks>
    public static RouteHandlerBuilder WithIdempotency(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<IdempotencyFilter>();
    }

}
