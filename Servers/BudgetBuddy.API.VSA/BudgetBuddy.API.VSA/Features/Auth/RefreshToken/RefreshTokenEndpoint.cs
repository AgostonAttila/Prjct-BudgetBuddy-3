namespace BudgetBuddy.API.VSA.Features.Auth.RefreshToken;

public class RefreshTokenEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // ============================================================================
        // SECURE REFRESH TOKEN ENDPOINT (⚠️ CURRENTLY NOT COMPATIBLE!)
        // ============================================================================
        // URL: /auth/token/refresh (different from built-in /auth/refresh)
        //
        // WHY THIS IS BETTER:
        // - Built-in /auth/refresh has NO reuse detection (OWASP security vulnerability)
        // - This secure version adds:
        //   • Token rotation (new refresh token on every refresh)
        //   • Token reuse detection (security breach alerts)
        //   • Token family revocation (if reuse detected, all tokens revoked)
        //   • Full audit trail in database
        //
        // ⚠️ PROBLEM - INCOMPATIBILITY:
        // - Built-in /auth/login saves refresh tokens in MEMORY (BearerTokenStore)
        // - This endpoint looks for tokens in the DATABASE
        // - Result: Tokens from built-in login won't work with this endpoint!
        //
        // 🔧 SOLUTION (to use this endpoint):
        // 1. Implement custom /auth/login that saves refresh tokens to DB
        // 2. Implement custom /auth/register endpoint
        // 3. Implement custom /auth/logout endpoint
        // 4. Enable middleware to block built-in /auth/refresh
        //    (See: Common/Middlewares/DisableInsecureRefreshEndpointMiddleware.cs)
        //
        // CURRENT STATE:
        // - Using built-in /auth/login + /auth/refresh (NO reuse detection!)
        // - This endpoint exists but will return "Invalid refresh token" error
        // ============================================================================
        
        
        //TODO  Expired token cleanup job
        //TODO  Concurrent token limit 
        //TODO  Session anomaly detection 
        //TODO  RS256
        //TODO migration
     
        
        app.MapPost("/auth/token/refresh", async (
            RefreshTokenCommand command,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithSummary("Refresh access token (SECURE - with reuse detection)")
        .WithDescription(
            "⚠️ INCOMPATIBLE with built-in /auth/login! This endpoint looks for tokens in the DATABASE, " +
            "but built-in login saves tokens in MEMORY. " +
            "To use this endpoint, implement custom login that saves tokens to DB. " +
            "\n\n" +
            "🔒 Security features (when properly configured): " +
            "Token rotation + Reuse detection + Token family revocation. " +
            "If token reuse is detected, all tokens for the user are revoked. " +
            "\n\n" +
            "📌 See endpoint comments for implementation requirements.")
        .WithTags("Authentication")
        .WithName("RefreshTokenSecure")
        .WithRefreshRateLimit()  // ✅ Brute force protection: 20 attempts per 15 min per IP
        .AllowAnonymous(); // No authentication required (refresh token is the auth)
    }
}
