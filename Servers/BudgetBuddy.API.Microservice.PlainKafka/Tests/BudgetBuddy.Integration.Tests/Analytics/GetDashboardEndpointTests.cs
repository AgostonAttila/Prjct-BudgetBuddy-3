using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Service.Analytics.Features.Dashboard.GetDashboard;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Analytics;

[Collection(nameof(AnalyticsApiCollection))]
public class GetDashboardEndpointTests(AnalyticsApiFactory factory) : IAsyncLifetime
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
    public async Task GetDashboard_ReturnsOk(string currency)
    {
        var response = await _client.GetAsync($"/api/dashboard?displayCurrency={currency}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("HUF")]
    public async Task GetDashboard_AccountsSummaryHasTwoAccounts(string currency)
    {
        var response = await _client.GetAsync($"/api/dashboard?displayCurrency={currency}");
        var body = await response.Content.ReadFromJsonAsync<GetDashboardResponse>();

        body!.AccountsSummary.AccountCount.Should().Be(2);
        body.DisplayCurrency.Should().Be(currency);
    }

    [Fact]
    public async Task GetDashboard_AccountsTotalBalance_Usd_IsCorrect()
    {
        var response = await _client.GetAsync("/api/dashboard?displayCurrency=USD");
        var body = await response.Content.ReadFromJsonAsync<GetDashboardResponse>();

        // TotalBalance = transaction-adjusted account balance + investment value
        // Accounts: Account1(1000+3000-1500=2500) + Account2(1000) = 3500 USD
        // Investments: AAPL(10×175) + MSFT(5×320) = 3350 USD → total: 6850 USD
        body!.AccountsSummary.TotalBalance
            .Should().BeApproximately(TestDataSeeder.ExpectedDashboardTotalBalanceUsd, 0.02m);
    }

    [Fact]
    public async Task GetDashboard_AccountsTotalBalance_Huf_IsCorrect()
    {
        var response = await _client.GetAsync("/api/dashboard?displayCurrency=HUF");
        var body = await response.Content.ReadFromJsonAsync<GetDashboardResponse>();

        // 6850 USD × 370.50 = 2537925 HUF
        body!.AccountsSummary.TotalBalance
            .Should().BeApproximately(TestDataSeeder.ExpectedDashboardTotalBalanceHuf, 1m);
    }
}
