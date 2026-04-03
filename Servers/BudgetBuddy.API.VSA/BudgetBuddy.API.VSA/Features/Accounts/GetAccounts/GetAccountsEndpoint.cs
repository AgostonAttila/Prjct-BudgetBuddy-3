
namespace BudgetBuddy.API.VSA.Features.Accounts.GetAccounts;

public class GetAccountsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/accounts", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAccountsQuery();
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get all accounts")
        .WithDescription("Retrieves all financial accounts belonging to the authenticated user, including their balances and currency information.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .CacheOutput("accounts-list")
        .WithTags("Accounts")
        .WithName("GetAccounts")
        ;
    }
}
