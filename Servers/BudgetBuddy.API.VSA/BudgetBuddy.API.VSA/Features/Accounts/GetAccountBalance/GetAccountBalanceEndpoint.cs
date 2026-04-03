namespace BudgetBuddy.API.VSA.Features.Accounts.GetAccountBalance;

public class GetAccountBalanceEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/accounts/{id:guid}/balance", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) => Results.Ok((object?)await mediator.Send(new GetAccountBalanceQuery(id), cancellationToken)))
        .WithSummary("Get account balance")
        .WithDescription("Retrieves the current balance for a specific account by calculating all transactions.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .CacheOutput("account-balance")
        .WithTags("Accounts")
        .WithName("GetAccountBalance");
    }
}
