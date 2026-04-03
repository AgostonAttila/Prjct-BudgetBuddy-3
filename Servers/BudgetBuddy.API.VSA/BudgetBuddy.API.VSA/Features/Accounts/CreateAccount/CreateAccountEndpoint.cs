using Mapster;

namespace BudgetBuddy.API.VSA.Features.Accounts.CreateAccount;

public class CreateAccountEndpoint : ICarterModule
{
    public record CreateAccountRequest(
        string Name,
        string Description,
        string DefaultCurrencyCode,
        decimal InitialBalance
    );

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/accounts", async (
            CreateAccountRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<CreateAccountCommand>();

            var result = await mediator.Send(command, cancellationToken);
            return Results.Created($"/api/accounts/{result.Id}", result);
        })
        .WithSummary("Create a new account")
        .WithDescription("Creates a new financial account. Include an Idempotency-Key header to prevent duplicate accounts.")
        .WithIdempotency()  
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Accounts")
        .WithName("CreateAccount");
    }
}


