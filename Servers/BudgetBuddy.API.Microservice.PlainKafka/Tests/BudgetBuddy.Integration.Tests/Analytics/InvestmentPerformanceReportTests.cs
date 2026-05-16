using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Service.Analytics.Features.Reports.GetInvestmentPerformance;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Analytics;

[Collection(nameof(AnalyticsApiCollection))]
public class InvestmentPerformanceReportTests(AnalyticsApiFactory factory) : IAsyncLifetime
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
    public async Task InvestmentPerformance_ReturnsTwoInvestments(string currency)
    {
        var response = await _client.GetAsync(
            $"/api/reports/investment-performance?startDate=2026-01-01&endDate=2026-05-15&displayCurrency={currency}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<InvestmentPerformanceResponse>(TestJsonOptions.Default);
        body.Should().NotBeNull();
        body!.Currency.Should().Be(currency);
        body.Investments.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("HUF")]
    public async Task InvestmentPerformance_CurrentValueMatchesPriceSnapshot(string currency)
    {
        var response = await _client.GetAsync(
            $"/api/reports/investment-performance?startDate=2026-01-01&endDate=2026-05-15&displayCurrency={currency}");
        var body = await response.Content.ReadFromJsonAsync<InvestmentPerformanceResponse>(TestJsonOptions.Default);

        // Expected: AAPL 10 × $175 + MSFT 5 × $320 in target currency
        var expectedCurrentUsd = TestDataSeeder.ExpectedInvestmentValueUsd;
        var expectedCurrent    = currency == "USD"
            ? expectedCurrentUsd
            : expectedCurrentUsd * TestDataSeeder.HufRate;

        body!.CurrentValue.Should().BeApproximately(expectedCurrent, currency == "USD" ? 0.02m : 1m);
    }

    [Fact]
    public async Task InvestmentPerformance_AaplGainIsPositive_Usd()
    {
        var response = await _client.GetAsync(
            "/api/reports/investment-performance?startDate=2026-01-01&endDate=2026-05-15&displayCurrency=USD");
        var body = await response.Content.ReadFromJsonAsync<InvestmentPerformanceResponse>(TestJsonOptions.Default);

        var aapl = body!.Investments.Single(i => i.Symbol == "AAPL");
        aapl.GainLoss.Should().BeGreaterThan(0);    // $175 > $150 purchase price
        aapl.GainLossPercentage.Should().BeGreaterThan(0);
    }
}
