using BudgetBuddy.Application.Features.Accounts.CreateAccount;
using BudgetBuddy.Application.Features.Accounts.DeleteAccount;
using BudgetBuddy.Application.Features.Accounts.GetAccountBalance;
using BudgetBuddy.Application.Features.Accounts.GetAccounts;
using BudgetBuddy.Application.Features.Accounts.UpdateAccount;
using Mapster;

namespace BudgetBuddy.API.Endpoints;

public record CreateAccountRequest(string Name, string Description, string DefaultCurrencyCode, decimal InitialBalance);
public record UpdateAccountRequest(string Name, string Description, string DefaultCurrencyCode, decimal InitialBalance);

public class AccountModule : CarterModule
{
    public AccountModule() : base("/api/accounts")
    {
        WithTags("Accounts");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("", GetAccounts)
            .WithSummary("Get all accounts")
            .WithDescription("Retrieves all financial accounts belonging to the authenticated user, including their balances and currency information.")
            .WithStandardRateLimit()
            .CacheOutput("accounts-list")
            .WithName("GetAccounts");

        app.MapPost("", CreateAccount)
            .WithSummary("Create a new account")
            .WithDescription("Creates a new financial account. Include an Idempotency-Key header to prevent duplicate accounts.")
            .WithIdempotency()
            .WithStandardRateLimit()
            .WithName("CreateAccount");

        app.MapPut("{id:guid}", UpdateAccount)
            .WithSummary("Update an account")
            .WithDescription("Updates an existing account's information including name, description, and default currency code.")
            .WithStandardRateLimit()
            .WithName("UpdateAccount");

        app.MapDelete("{id:guid}", DeleteAccount)
            .WithSummary("Delete an account")
            .WithDescription("Permanently deletes an account. Warning: This will also delete all associated transactions.")
            .WithStandardRateLimit()
            .WithName("DeleteAccount");

        app.MapGet("{id:guid}/balance", GetAccountBalance)
            .WithSummary("Get account balance")
            .WithDescription("Retrieves the current balance for a specific account by calculating all transactions.")
            .WithStandardRateLimit()
            .CacheOutput("account-balance")
            .WithName("GetAccountBalance");
    }

    private static async Task<IResult> GetAccounts(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAccountsQuery(), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateAccount(CreateAccountRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<CreateAccountCommand>();
        var result = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/accounts/{result.Id}", result);
    }

    private static async Task<IResult> UpdateAccount(Guid id, UpdateAccountRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        var command = request.Adapt<UpdateAccountCommand>() with { Id = id };
        var result = await mediator.Send(command, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> DeleteAccount(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteAccountCommand(id), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetAccountBalance(Guid id, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAccountBalanceQuery(id), cancellationToken);
        return Results.Ok(result);
    }
}
