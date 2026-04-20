using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BudgetBuddy.Module.Auth.Infrastructure.Middlewares;

/// <summary>
/// Middleware that checks if the JWT token has been revoked/blacklisted.
/// Should be placed after authentication middleware.
/// </summary>
public class TokenBlacklistMiddleware(RequestDelegate next, ILogger<TokenBlacklistMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService blacklistService)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();

        try
        {
            var isRevoked = await blacklistService.IsTokenRevokedAsync(token, context.RequestAborted);

            if (isRevoked)
            {
                logger.LogWarning("Revoked token detected for user {UserId}",
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Token has been revoked",
                    message = "Your session has been terminated. Please log in again."
                });
                return;
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var issuedAtClaim = context.User.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
                if (long.TryParse(issuedAtClaim, out var issuedAtUnix))
                {
                    var issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnix);
                    var allRevoked = await blacklistService.AreAllUserTokensRevokedAsync(userId, issuedAt, context.RequestAborted);

                    if (allRevoked)
                    {
                        logger.LogWarning("All tokens revoked for user {UserId}, token issued at {IssuedAt}", userId, issuedAt);

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "All sessions have been revoked",
                            message = "All your sessions have been terminated. Please log in again."
                        });
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking token blacklist");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Authentication error",
                message = "Unable to verify token validity. Please try again."
            });
            return;
        }

        await next(context);
    }
}

public static class TokenBlacklistMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenBlacklist(this IApplicationBuilder builder)
        => builder.UseMiddleware<TokenBlacklistMiddleware>();
}
