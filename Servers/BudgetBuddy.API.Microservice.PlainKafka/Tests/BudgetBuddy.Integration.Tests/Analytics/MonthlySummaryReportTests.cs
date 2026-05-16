using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Service.Analytics.Features.Reports.GetMonthlySummary;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Analytics;

[Collection(nameof(AnalyticsApiCollection))]
public class MonthlySummaryReportTests(AnalyticsApiFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        await TestDataSeeder.SeedAnalyticsAsync(factory.Services);
        _client = factory.CreateAuthenticatedClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("HUF")]
    public async Task MonthlySummary_April_CorrectExpense(string currency)
    {
        var response = await _client.GetAsync(
            $"/api/reports/monthly-summary?year=2026&month=4&displayCurrency={currency}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<MonthlySummaryResponse>();
        body.Should().NotBeNull();
        body!.Year.Should().Be(2026);
        body.Month.Should().Be(4);
        body.Currency.Should().Be(currency);

        if (currency == "USD")
        {
            body.TotalExpense.Should().BeApproximately(TestDataSeeder.ExpectedAprilExpense, 0.02m);
            body.TotalIncome.Should().Be(0m);
        }
        else // HUF
        {
            body.TotalExpense.Should().BeApproximately(TestDataSeeder.ExpectedAprilExpenseHuf, 1m);
            body.TotalIncome.Should().Be(0m);
        }
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("HUF")]
    public async Task MonthlySummary_January_CorrectIncome(string currency)
    {
        var response = await _client.GetAsync(
            $"/api/reports/monthly-summary?year=2026&month=1&displayCurrency={currency}");
        var body = await response.Content.ReadFromJsonAsync<MonthlySummaryResponse>();

        if (currency == "USD")
        {
            body!.TotalIncome.Should().BeApproximately(TestDataSeeder.ExpectedTotalIncome, 0.02m);
            body.TotalExpense.Should().Be(0m);
        }
        else
        {
            body!.TotalIncome.Should().BeApproximately(TestDataSeeder.ExpectedTotalIncomeHuf, 1m);
        }
    }

    [Fact]
    public async Task MonthlySummary_April_NetIncomeIsNegative_Usd()
    {
        var response = await _client.GetAsync(
            "/api/reports/monthly-summary?year=2026&month=4&displayCurrency=USD");
        var body = await response.Content.ReadFromJsonAsync<MonthlySummaryResponse>();

        body!.NetIncome.Should().BeApproximately(-TestDataSeeder.ExpectedAprilExpense, 0.02m);
    }
}
