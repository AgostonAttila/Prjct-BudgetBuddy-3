using Carter;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NodaTime;

namespace BudgetBuddy.Service.Investments.Features.Investments.SellInvestment;

public class SellInvestmentEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/investments/sell", async (
            SellInvestmentRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = request.Adapt<SellInvestmentCommand>();
            var result  = await mediator.Send(command, cancellationToken);
            return Results.Ok(result);
        })
        .WithSummary("Sell an investment (FIFO)")
        .WithDescription(
            "Allocates a sell order against the oldest purchase lots first (FIFO). " +
            "Returns the realized gain/loss across all consumed lots.")
        .WithStandardRateLimit()
        .RequireAuthorization()
        .WithTags("Investments")
        .WithName("SellInvestment");
    }
}

public record SellInvestmentRequest(
    string Symbol,
    decimal QuantityToSell,
    decimal SalePrice,
    LocalDate SaleDate);
