using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BudgetBuddy.API.Filters;

/// <summary>
/// Endpoint filter that prevents duplicate requests using Idempotency-Key header
/// </summary>
public class IdempotencyFilter(
    IDistributedCache cache,
    ILogger<IdempotencyFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        // Get Idempotency-Key from header
        var idempotencyKey = httpContext.Request.Headers["Idempotency-Key"].ToString();

        if (string.IsNullOrEmpty(idempotencyKey))
        {
            // Idempotency key is optional, but recommended
            logger.LogWarning(
                "Request to {Path} without Idempotency-Key header. Consider adding it to prevent duplicates.",
                httpContext.Request.Path);

            // Continue without idempotency protection
            return await next(context);
        }

        // Validate idempotency key format (should be a GUID or similar unique identifier)
        if (idempotencyKey.Length < 16 || idempotencyKey.Length > 128)
        {
            return Results.BadRequest(new
            {
                error = "Invalid Idempotency-Key",
                message = "Idempotency-Key must be between 16 and 128 characters"
            });
        }

        // Build cache key with user context to prevent cross-user key collisions
        // Try multiple claim types (different auth schemes use different claim names)
        var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? httpContext.User?.FindFirst("sub")?.Value
                     ?? httpContext.User?.FindFirst("userId")?.Value;

        // We cannot safely use idempotency without a user context to prevent cross-user collisions
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning(
                "Idempotency-Key provided but user is not authenticated. Rejecting request for security. User: {User}, Claims: {Claims}",
                httpContext.User?.Identity?.Name ?? "null",
                httpContext.User?.Claims.Select(c => $"{c.Type}={c.Value}").ToArray() ?? Array.Empty<string>());
            return Results.Unauthorized();
        }

        var cacheKey = $"idempotency:{userId}:{idempotencyKey}";

        // Check if we've seen this key before
        var cachedResponse = await cache.GetStringAsync(cacheKey);

        if (cachedResponse != null)
        {
            logger.LogInformation(
                "Idempotent request detected for user {UserId}. Returning cached response for key: {IdempotencyKey}",
                userId,
                idempotencyKey);

            // Deserialize and return cached response
            try
            {
                var response = JsonSerializer.Deserialize<CachedResponse>(cachedResponse);

                if (response != null)
                {
                    // Return the same status code and body as the original request
                    httpContext.Response.StatusCode = response.StatusCode;
                    return Results.Json(response.Body, statusCode: response.StatusCode);
                }
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to deserialize cached idempotency response");
                // Continue to execute the request if cache is corrupted
            }
        }

        // Execute the request
        var result = await next(context);

        // Cache successful responses (2xx status codes)
        if (result is IStatusCodeHttpResult statusCodeResult)
        {
            var statusCode = statusCodeResult.StatusCode ?? 200;

            if (statusCode >= 200 && statusCode < 300)
            {
                try
                {
                    var cachedData = new CachedResponse
                    {
                        StatusCode = statusCode,
                        Body = result
                    };

                    var serializedResult = JsonSerializer.Serialize(cachedData);

                    await cache.SetStringAsync(
                        cacheKey,
                        serializedResult,
                        new DistributedCacheEntryOptions
                        {
                            // Cache for 24 hours
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                        });

                    logger.LogInformation(
                        "Cached response for idempotency key: {IdempotencyKey} (expires in 24h)",
                        idempotencyKey);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to cache idempotency response");
                    // Don't fail the request if caching fails
                }
            }
            else
            {
                logger.LogWarning(
                    "Not caching response with status code {StatusCode} for idempotency key: {IdempotencyKey}",
                    statusCode,
                    idempotencyKey);
            }
        }

        return result;
    }

    private class CachedResponse
    {
        public int StatusCode { get; set; }
        public object? Body { get; set; }
    }
}
