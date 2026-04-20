namespace BudgetBuddy.Module.Auth.Features.TwoFactor.GetTwoFactorStatus;

public class GetTwoFactorStatusEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/twofactor/status", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTwoFactorStatusQuery(context.User);
            var result = await mediator.Send(query, cancellationToken);
            return Results.Ok(result);
        })
        .WithSummary("Get two-factor authentication status")
        .WithDescription("Returns whether 2FA is enabled for the current user")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("TwoFactor")
        .WithName("GetTwoFactorStatus")
        ;
    }
}
