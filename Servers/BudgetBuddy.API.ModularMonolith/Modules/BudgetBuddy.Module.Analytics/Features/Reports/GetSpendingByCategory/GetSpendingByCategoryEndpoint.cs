using NodaTime.Text;

namespace BudgetBuddy.Module.Analytics.Features.Reports.GetSpendingByCategory;

public class GetSpendingByCategoryEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/spending-by-category", async (
            IMediator mediator,
            string? startDate,
            string? endDate,
            Guid? accountId,
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

            var query = new GetSpendingByCategoryQuery(parsedStartDate, parsedEndDate, accountId, displayCurrency);
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get spending by category report")
        .WithDescription("Generates a breakdown of spending by category for a specified date range and optional account filter. Amounts can be converted to a display currency using the displayCurrency parameter.")
        .WithReportRateLimit()
        .RequireAuthorization()
        .CacheOutput("spending-by-category")
        .WithTags("Reports")
        .WithName("GetSpendingByCategory")
        ;
    }
}
