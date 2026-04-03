using NodaTime.Text;

namespace BudgetBuddy.API.VSA.Features.Reports.GetIncomeVsExpense;

public class GetIncomeVsExpenseEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reports/income-vs-expense", async (
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

            var query = new GetIncomeVsExpenseQuery(parsedStartDate, parsedEndDate, accountId, displayCurrency);
            var result = await mediator.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithSummary("Get income vs expense report")
        .WithDescription("Generates a comparison report of income versus expenses for a specified date range and optional account filter. Amounts can be converted to a display currency using the displayCurrency parameter.")
        .WithReportRateLimit()
        .RequireAuthorization()
        .CacheOutput("income-vs-expense")
        .WithTags("Reports")
        .WithName("GetIncomeVsExpense")
        ;
    }
}
