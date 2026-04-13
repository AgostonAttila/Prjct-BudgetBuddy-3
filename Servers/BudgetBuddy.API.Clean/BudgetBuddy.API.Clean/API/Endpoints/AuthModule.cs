using BudgetBuddy.Application.Features.Auth.RefreshToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BudgetBuddy.API.Endpoints;

public class AuthModule : CarterModule
{
    public AuthModule() : base("/auth")
    {
        WithTags("Authentication");
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        // ============================================================================
        // SECURE REFRESH TOKEN ENDPOINT (⚠️ CURRENTLY NOT COMPATIBLE!)
        // ============================================================================
        // Built-in /auth/login saves refresh tokens in MEMORY (BearerTokenStore)
        // This endpoint looks for tokens in the DATABASE — incompatible until
        // a custom login/register/logout implementation replaces the built-in ones.
        // See: Features/Auth/RefreshToken/ for the implementation requirements.
        // ============================================================================
        app.MapPost("token/refresh", RefreshToken)
            .WithSummary("Refresh access token (SECURE - with reuse detection)")
            .WithDescription("⚠️ INCOMPATIBLE with built-in /auth/login. Requires custom login that saves tokens to DB. Security features: Token rotation + Reuse detection + Token family revocation.")
            .WithRefreshRateLimit()
            .AllowAnonymous()
            .WithName("RefreshTokenSecure");

        app.MapPost("logout-with-revoke", LogoutWithRevoke)
            .WithSummary("Logout and revoke token")
            .WithDescription("Logs out the user and revokes the JWT token if present. Use this instead of /auth/logout for better security.")
            .WithAuthRateLimit()
            .RequireAuthorization()
            .WithName("LogoutWithRevoke");

        app.MapPost("logout-all", LogoutAll)
            .WithSummary("Logout from all devices")
            .WithDescription("Revokes all JWT tokens for the user across all devices and logs out the current session.")
            .WithAuthRateLimit()
            .RequireAuthorization()
            .WithName("LogoutAll");
    }

    private static async Task<IResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var command = new RefreshTokenCommand(request.RefreshToken, ipAddress);
        var result = await mediator.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private record RefreshTokenRequest(string RefreshToken);

    private static async Task<IResult> LogoutWithRevoke(
        HttpContext context,
        [FromServices] SignInManager<User> signInManager,
        [FromServices] ITokenBlacklistService blacklistService,
        [FromServices] ILogger<AuthModule> logger)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            try
            {
                await blacklistService.RevokeTokenAsync(token);
                logger.LogInformation("JWT token revoked for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to revoke JWT token for user {UserId}", userId);
            }
        }

        await signInManager.SignOutAsync();
        logger.LogInformation("User {UserId} logged out", userId);
        return Results.Ok(new { message = "Logged out successfully" });
    }

    private static async Task<IResult> LogoutAll(
        HttpContext context,
        [FromServices] SignInManager<User> signInManager,
        [FromServices] ITokenBlacklistService blacklistService,
        [FromServices] ILogger<AuthModule> logger)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        try
        {
            await blacklistService.RevokeAllUserTokensAsync(userId);
            logger.LogWarning("All tokens revoked for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke all tokens for user {UserId}", userId);
            return Results.Problem("Failed to revoke all sessions");
        }

        await signInManager.SignOutAsync();
        logger.LogInformation("User {UserId} logged out from all devices", userId);
        return Results.Ok(new { message = "Logged out from all devices successfully", revokedDevices = "all" });
    }
}
