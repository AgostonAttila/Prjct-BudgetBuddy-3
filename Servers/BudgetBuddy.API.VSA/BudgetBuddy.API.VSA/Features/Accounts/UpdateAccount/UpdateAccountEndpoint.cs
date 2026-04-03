
using Mapster;

namespace BudgetBuddy.API.VSA.Features.Accounts.UpdateAccount;

public class UpdateAccountEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/accounts/{id:guid}", async (
            Guid id,
            UpdateAccountRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            
            var command = request.Adapt<UpdateAccountCommand>() with { Id = id };
           

            var result = await mediator.Send(command, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Update an account")
        .WithDescription("Updates an existing account's information including name, description, and default currency code.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Accounts")
        .WithName("UpdateAccount")
        ;
    }
}

public record UpdateAccountRequest(
    string Name,
    string Description,
    string DefaultCurrencyCode,
    decimal InitialBalance
);
