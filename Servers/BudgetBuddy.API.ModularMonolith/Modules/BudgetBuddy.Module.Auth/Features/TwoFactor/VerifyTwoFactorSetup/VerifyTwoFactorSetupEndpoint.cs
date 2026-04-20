using BudgetBuddy.Shared.Infrastructure.Filters;

namespace BudgetBuddy.Module.Auth.Features.TwoFactor.VerifyTwoFactorSetup;

public class VerifyTwoFactorSetupEndpoint : ICarterModule
{
    private record VerifyTwoFactorSetupRequest(string Code);

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/twofactor/verify", async (
            VerifyTwoFactorSetupRequest request,
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new VerifyTwoFactorSetupCommand(context.User, request.Code);
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithSummary("Verify two-factor authentication setup")
        .WithDescription("Verifies the TOTP code from authenticator app and enables 2FA. Protected against brute force attacks (5 attempts per 15 minutes).")
        .WithTwoFactorRateLimit() // Brute force protection
        .RequireAuthorization()
        .WithTags("TwoFactor")
        .WithName("VerifyTwoFactorSetup")
        ;
    }
}
