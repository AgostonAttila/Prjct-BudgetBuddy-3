namespace BudgetBuddy.API.VSA.Features.TwoFactor.EnableTwoFactor;

public class EnableTwoFactorEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/twofactor/enable", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new EnableTwoFactorCommand(context.User);
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithSummary("Enable two-factor authentication")
        .WithDescription("Generates QR code and shared key for setting up TOTP authenticator app")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("TwoFactor")
        .WithName("EnableTwoFactor")
        ;
    }
}
