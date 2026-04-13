using BudgetBuddy.Application.Features.Investments.GetPortfolioValue;

namespace BudgetBuddy.API.Endpoints;

public class PortfolioModule : CarterModule
{
    public PortfolioModule() : base("/api/portfolio")
    {
        WithTags("Portfolio");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("value", GetPortfolioValue)
            .WithSummary("Get portfolio value")
            .WithDescription("Calculates the total current value of the investment portfolio in the specified currency.")
            .WithStandardRateLimit()
            .CacheOutput("portfolio-value")
            .WithName("GetPortfolioValue");
    }

    private static async Task<IResult> GetPortfolioValue(IMediator mediator, string? currency = null, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetPortfolioValueQuery(currency), cancellationToken);
        return Results.Ok(result);
    }
}
