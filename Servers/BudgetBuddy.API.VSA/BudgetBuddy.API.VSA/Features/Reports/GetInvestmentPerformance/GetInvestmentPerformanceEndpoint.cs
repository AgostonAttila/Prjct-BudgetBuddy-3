using NodaTime.Text;

namespace BudgetBuddy.API.VSA.Features.Reports.GetInvestmentPerformance;

public class GetInvestmentPerformanceEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/investment-performance", async (
            IMediator mediator,
            string? startDate,
            string? endDate,
            InvestmentType? type,
            string? displayCurrency,
            CancellationToken cancellationToken) =>
        {
            LocalDate? parsedStartDate = null;
            LocalDate? parsedEndDate = null;

            if (!string.IsNullOrEmpty(startDate))
            {
                var parseResult = LocalDatePattern.Iso.Parse(startDate);
                if (parseResult.Success)
                    parsedStartDate = parseResult.Value;
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                var parseResult = LocalDatePattern.Iso.Parse(endDate);
                if (parseResult.Success)
                    parsedEndDate = parseResult.Value;
            }

            var query = new GetInvestmentPerformanceQuery(parsedStartDate, parsedEndDate, type, displayCurrency);
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get investment performance report")
        .WithDescription("Generates an investment performance report with gain/loss analysis for a specified date range and optional investment type filter. Amounts can be converted to a display currency using the displayCurrency parameter.")
        .WithReportRateLimit()
        .RequireAuthorization()
        .CacheOutput("investment-performance")
        .WithTags("Reports")
        .WithName("GetInvestmentPerformance")
        ;
    }
}
