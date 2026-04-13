using BudgetBuddy.Application.Features.Reports.GetIncomeVsExpense;
using BudgetBuddy.Application.Features.Reports.GetInvestmentPerformance;
using BudgetBuddy.Application.Features.Reports.GetMonthlySummary;
using BudgetBuddy.Application.Features.Reports.GetSpendingByCategory;
using NodaTime.Text;

namespace BudgetBuddy.API.Endpoints;

public class ReportModule : CarterModule
{
    public ReportModule() : base("/api/reports")
    {
        WithTags("Reports");
        RequireAuthorization();
    }

    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("income-vs-expense", GetIncomeVsExpense)
            .WithSummary("Get income vs expense report")
            .WithDescription("Generates a comparison report of income versus expenses for a specified date range and optional account filter.")
            .WithReportRateLimit()
            .CacheOutput("income-vs-expense")
            .WithName("GetIncomeVsExpense");

        app.MapGet("investment-performance", GetInvestmentPerformance)
            .WithSummary("Get investment performance report")
            .WithDescription("Generates an investment performance report with gain/loss analysis for a specified date range and optional investment type filter.")
            .WithReportRateLimit()
            .CacheOutput("investment-performance")
            .WithName("GetInvestmentPerformance");

        app.MapGet("monthly-summary", GetMonthlySummary)
            .WithSummary("Get monthly summary report")
            .WithDescription("Generates a comprehensive financial summary for a specific month and year with optional account filtering.")
            .WithReportRateLimit()
            .CacheOutput("monthly-summary")
            .WithName("GetMonthlySummary");

        app.MapGet("spending-by-category", GetSpendingByCategory)
            .WithSummary("Get spending by category report")
            .WithDescription("Generates a breakdown of spending by category for a specified date range and optional account filter.")
            .WithReportRateLimit()
            .CacheOutput("spending-by-category")
            .WithName("GetSpendingByCategory");
    }

    private static async Task<IResult> GetIncomeVsExpense(
        IMediator mediator, string? startDate, string? endDate, Guid? accountId, string? displayCurrency, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetIncomeVsExpenseQuery(ParseDate(startDate), ParseDate(endDate), accountId, displayCurrency), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetInvestmentPerformance(
        IMediator mediator, string? startDate, string? endDate, InvestmentType? type, string? displayCurrency, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInvestmentPerformanceQuery(ParseDate(startDate), ParseDate(endDate), type, displayCurrency), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetMonthlySummary(
        IMediator mediator, int year, int month, Guid? accountId, string? displayCurrency, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMonthlySummaryQuery(year, month, accountId, displayCurrency), cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetSpendingByCategory(
        IMediator mediator, string? startDate, string? endDate, Guid? accountId, string? displayCurrency, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetSpendingByCategoryQuery(ParseDate(startDate), ParseDate(endDate), accountId, displayCurrency), cancellationToken);
        return Results.Ok(result);
    }

    private static LocalDate? ParseDate(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        var r = LocalDatePattern.Iso.Parse(value);
        return r.Success ? r.Value : null;
    }
}
