namespace BudgetBuddy.API.VSA.Features.TwoFactor.DisableTwoFactor;

public class DisableTwoFactorEndpoint : ICarterModule
{
    private record DisableTwoFactorRequest(string Password);

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/twofactor/disable", async (
            DisableTwoFactorRequest request,
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new DisableTwoFactorCommand(context.User, request.Password);
            var result = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithSummary("Disable two-factor authentication")
        .WithDescription("Disables 2FA for the current user (requires password confirmation)")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("TwoFactor")
        .WithName("DisableTwoFactor")
        ;
    }
}
