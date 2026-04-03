using Mapster;

namespace BudgetBuddy.API.VSA.Features.Investments.UpdateInvestment;

public class UpdateInvestmentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/api/investments/{id:guid}", async (
            Guid id,
            UpdateInvestmentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<UpdateInvestmentCommand>() with { Id = id };

            var result = await mediator.Send(command, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Update an investment")
        .WithDescription("Updates an existing investment's details including symbol, quantity, purchase price, and date.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Investments")
        .WithName("UpdateInvestment")
        ;
    }
}

public record UpdateInvestmentRequest(
    string Symbol,
    string Name,
    InvestmentType Type,
    decimal Quantity,
    decimal PurchasePrice,
    string CurrencyCode,
    LocalDate PurchaseDate,
    string? Note,
    Guid? AccountId
);
