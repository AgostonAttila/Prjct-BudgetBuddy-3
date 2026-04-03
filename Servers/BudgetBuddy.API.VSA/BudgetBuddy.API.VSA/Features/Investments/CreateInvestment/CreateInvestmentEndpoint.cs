using Mapster;

namespace BudgetBuddy.API.VSA.Features.Investments.CreateInvestment;

public class CreateInvestmentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/investments", async (
            CreateInvestmentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<CreateInvestmentCommand>();

            var result = await mediator.Send(command, cancellationToken);

            return Results.Created($"/api/investments/{result.Id}", result);
        })
        .WithSummary("Create a new investment")
        .WithDescription("Creates a new investment record. Include an Idempotency-Key header to prevent duplicate purchases.")
        .WithIdempotency()  // Prevent duplicate investment purchases
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Investments")
        .WithName("CreateInvestment")
        ;
    }
}

public record CreateInvestmentRequest(
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
