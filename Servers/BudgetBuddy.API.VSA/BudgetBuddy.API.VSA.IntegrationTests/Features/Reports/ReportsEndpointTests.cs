using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.IntegrationTests.Features.Reports;

[Collection("Integration")]
public class ReportsEndpointTests(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = await AuthenticatedHttpClient.CreateAsync(factory,
            email: $"reports_{Guid.NewGuid()}@test.com");
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GET_IncomeVsExpense_Returns200()
    {
        var response = await _client.GetAsync("/api/reports/income-vs-expense");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<IncomeVsExpenseResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GET_IncomeVsExpense_WithDateRange_Returns200()
    {
        var response = await _client.GetAsync("/api/reports/income-vs-expense?startDate=2026-01-01&endDate=2026-03-31&displayCurrency=HUF");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_SpendingByCategory_Returns200()
    {
        var response = await _client.GetAsync("/api/reports/spending-by-category");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SpendingByCategoryResponse>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GET_MonthlySummary_Returns200()
    {
        var response = await _client.GetAsync("/api/reports/monthly-summary?year=2026&month=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MonthlySummaryResponse>();
        result.Should().NotBeNull();
        result!.Year.Should().Be(2026);
        result.Month.Should().Be(3);
    }

    [Fact]
    public async Task GET_MonthlySummary_MissingYear_Returns400()
    {
        var response = await _client.GetAsync("/api/reports/monthly-summary?month=3");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_InvestmentPerformance_Returns200()
    {
        var response = await _client.GetAsync("/api/reports/investment-performance");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_Reports_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/reports/income-vs-expense");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record IncomeVsExpenseResponse(decimal TotalIncome, decimal TotalExpense, decimal NetIncome, string Currency, List<object>? MonthlyData);
    private sealed record SpendingByCategoryResponse(decimal TotalSpending, string Currency, List<object>? Categories);
    private sealed record MonthlySummaryResponse(int Year, int Month, string MonthName, decimal TotalIncome, decimal TotalExpense, string Currency);
}
