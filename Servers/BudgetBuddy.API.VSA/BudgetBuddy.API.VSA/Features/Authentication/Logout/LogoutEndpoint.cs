using System.Security.Claims;
using BudgetBuddy.API.VSA.Common.Infrastructure.Security;
using BudgetBuddy.API.VSA.Common.Infrastructure.Security.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BudgetBuddy.API.VSA.Features.Authentication.Logout;

/// <summary>
/// Custom logout endpoint that revokes the JWT token
/// This extends the built-in Identity API logout functionality
/// </summary>
public class LogoutEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Custom logout endpoint that also revokes JWT tokens
        app.MapPost("/auth/logout-with-revoke", async (
            HttpContext context,
            [FromServices] SignInManager<User> signInManager,
            [FromServices] ITokenBlacklistService blacklistService,
            [FromServices] ILogger<LogoutEndpoint> logger) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Get JWT token from Authorization header
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader["Bearer ".Length..].Trim();

                try
                {
                    // Revoke the JWT token
                    await blacklistService.RevokeTokenAsync(token);
                    logger.LogInformation("JWT token revoked for user {UserId}", userId);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to revoke JWT token for user {UserId}", userId);
                    // Continue with logout even if token revocation fails
                }
            }

            // Sign out (clears cookie)
            await signInManager.SignOutAsync();

            logger.LogInformation("User {UserId} logged out", userId);

            return Results.Ok(new { message = "Logged out successfully" });
        })
        .RequireAuthorization()
        .WithSummary("Logout and revoke token")
        .WithDescription("Logs out the user and revokes the JWT token if present. Use this instead of /auth/logout for better security.")
        .WithAuthRateLimit()
        .WithTags("Authentication")
        .WithName("LogoutWithRevoke");

        // Logout from all devices - revokes all user tokens
        app.MapPost("/auth/logout-all", async (
            HttpContext context,
            [FromServices] SignInManager<User> signInManager,
            [FromServices] ITokenBlacklistService blacklistService,
            [FromServices] ILogger<LogoutEndpoint> logger) =>
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                // Revoke ALL tokens for this user
                await blacklistService.RevokeAllUserTokensAsync(userId);
                logger.LogWarning("All tokens revoked for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to revoke all tokens for user {UserId}", userId);
                return Results.Problem("Failed to revoke all sessions");
            }

            // Sign out from current session
            await signInManager.SignOutAsync();

            logger.LogInformation("User {UserId} logged out from all devices", userId);

            return Results.Ok(new
            {
                message = "Logged out from all devices successfully",
                revokedDevices = "all"
            });
        })
        .RequireAuthorization()
        .WithSummary("Logout from all devices")
        .WithDescription("Revokes all JWT tokens for the user across all devices and logs out the current session.")
        .WithAuthRateLimit()
        .WithTags("Authentication")
        .WithName("LogoutAll");
    }
}
