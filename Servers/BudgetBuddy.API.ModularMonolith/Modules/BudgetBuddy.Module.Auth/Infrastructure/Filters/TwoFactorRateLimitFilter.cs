using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace BudgetBuddy.Module.Auth.Infrastructure.Filters;

/// <summary>
/// Rate limit filter for 2FA verification attempts
/// Prevents brute force attacks on 6-digit TOTP codes
/// </summary>
public class TwoFactorRateLimitFilter(
    IDistributedCache cache,
    ILogger<TwoFactorRateLimitFilter> logger)
    : IEndpointFilter
{
    // Configuration
    private const int MaxAttemptsPerWindow = 5;
    private const int WindowMinutes = 15;
    private const int LockoutMinutes = 30;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var cacheKey = $"2fa-attempts:{userId}";
        var lockoutKey = $"2fa-lockout:{userId}";

        // Check if user is locked out
        var isLockedOut = await cache.GetStringAsync(lockoutKey);
        if (!string.IsNullOrEmpty(isLockedOut))
        {
            var lockoutExpiry = DateTimeOffset.Parse(isLockedOut);
            var remainingMinutes = (int)(lockoutExpiry - DateTimeOffset.UtcNow).TotalMinutes;

            logger.LogWarning("User {UserId} is locked out from 2FA attempts until {Expiry}",
                userId, lockoutExpiry);

            return Results.Problem(
                title: "Too many failed attempts",
                detail: $"Account temporarily locked due to multiple failed 2FA attempts. Try again in {remainingMinutes} minutes.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        // Get current attempt count
        var attemptsStr = await cache.GetStringAsync(cacheKey);
        var attempts = string.IsNullOrEmpty(attemptsStr) ? 0 : int.Parse(attemptsStr);

        // Check if exceeded max attempts
        if (attempts >= MaxAttemptsPerWindow)
        {
            // Lock out the user
            var lockoutExpiry = DateTimeOffset.UtcNow.AddMinutes(LockoutMinutes);
            await cache.SetStringAsync(
                lockoutKey,
                lockoutExpiry.ToString("o"),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = lockoutExpiry
                });

            // Reset attempt counter
            await cache.RemoveAsync(cacheKey);

            logger.LogWarning("User {UserId} locked out from 2FA attempts for {Minutes} minutes after {Attempts} failed attempts",
                userId, LockoutMinutes, attempts);

            return Results.Problem(
                title: "Too many failed attempts",
                detail: $"Account locked for {LockoutMinutes} minutes due to multiple failed 2FA attempts.",
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        // Proceed with the request
        var result = await next(context);

        // Check if the verification failed (based on result type)
        var httpResult = result as IStatusCodeHttpResult;
        if (httpResult?.StatusCode == StatusCodes.Status400BadRequest ||
            httpResult?.StatusCode == StatusCodes.Status401Unauthorized)
        {
            // Increment failed attempt counter
            attempts++;
            await cache.SetStringAsync(
                cacheKey,
                attempts.ToString(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(WindowMinutes)
                });

            var remainingAttempts = MaxAttemptsPerWindow - attempts;
            logger.LogWarning(
                "Failed 2FA attempt {Attempt}/{Max} for user {UserId}. {Remaining} attempts remaining before lockout",
                attempts, MaxAttemptsPerWindow, userId, remainingAttempts);

            // Add warning header
            httpContext.Response.Headers["X-2FA-Attempts-Remaining"] = remainingAttempts.ToString();
        }
        else if (httpResult?.StatusCode == StatusCodes.Status200OK)
        {
            // Success - reset attempt counter
            await cache.RemoveAsync(cacheKey);
            logger.LogInformation("Successful 2FA verification for user {UserId}", userId);
        }

        return result;
    }
}

/// <summary>
/// Extension methods for 2FA rate limiting
/// </summary>
public static class TwoFactorRateLimitExtensions
{
    public static RouteHandlerBuilder WithTwoFactorRateLimit(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<TwoFactorRateLimitFilter>();
    }
}
