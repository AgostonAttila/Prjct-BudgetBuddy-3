namespace BudgetBuddy.API.VSA.Common.Extensions;

/// <summary>
/// Extension methods for applying rate limiting policies to endpoints
/// </summary>
public static class RateLimitingEndpointExtensions
{
    /// <summary>
    /// Applies auth rate limiting (Token Bucket - 10 tokens, 5/min replenishment)
    /// Use for: Authentication, password reset, email verification
    /// </summary>
    public static TBuilder WithAuthRateLimit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return (TBuilder)builder.RequireRateLimiting("auth");
    }

    /// <summary>
    /// Applies refresh token rate limiting (Fixed Window by IP - 20 attempts per 15 min per IP)
    /// Use for: Refresh token endpoints
    ///
    /// Protects against:
    /// - Brute force attacks (though refresh tokens are 512-bit random)
    /// - Stolen token abuse testing
    /// - DoS attacks
    ///
    /// Normal usage: ~30-50 refreshes per day (access token expires every 15 min)
    /// </summary>
    public static TBuilder WithRefreshRateLimit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return (TBuilder)builder.RequireRateLimiting("refresh");
    }

    /// <summary>
    /// Applies strict rate limiting (Fixed Window by IP - 450 req/min per IP)
    /// Use for: Batch operations, imports/exports, transfers, resource-intensive operations
    /// </summary>
    public static TBuilder WithStrictRateLimit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return (TBuilder)builder.RequireRateLimiting("fixedByIp");
    }

    /// <summary>
    /// Applies API rate limiting (Sliding Window - 500 req/min with 6 segments)
    /// Use for: Complex queries, reports, dashboard, aggregations
    /// </summary>
    public static TBuilder WithApiRateLimit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return (TBuilder)builder.RequireRateLimiting("api");
    }

    /// <summary>
    /// Applies standard rate limiting (Fixed Window - 300 req/min)
    /// Use for: Standard CRUD operations, simple GET requests
    /// </summary>
    public static TBuilder WithStandardRateLimit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return (TBuilder)builder.RequireRateLimiting("fixed");
    }

    /// <summary>
    /// Applies batch operation rate limiting (alias for WithStrictRateLimit)
    /// Use for: Batch updates, batch deletes
    /// </summary>
    public static TBuilder WithBatchOperationRateLimit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.WithStrictRateLimit();
    }

    /// <summary>
    /// Applies import/export rate limiting (alias for WithStrictRateLimit)
    /// Use for: File imports and exports
    /// </summary>
    public static TBuilder WithImportExportRateLimit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.WithStrictRateLimit();
    }

    /// <summary>
    /// Applies report rate limiting (alias for WithApiRateLimit)
    /// Use for: Complex reports and analytics endpoints
    /// </summary>
    public static TBuilder WithReportRateLimit<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.WithApiRateLimit();
    }
}
