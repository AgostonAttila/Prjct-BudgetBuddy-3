using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Service.Investments.Features.GetInvestments;
using BudgetBuddy.Service.Investments.Features.GetPortfolioValue;
using BudgetBuddy.Shared.Messages.Contracts.Investments;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Investments;

[Collection(nameof(InvestmentsApiCollection))]
public class GetPortfolioValueEndpointTests(InvestmentsApiFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
        await TestDataSeeder.SeedInvestmentsAsync(factory.Services);
        _client = factory.CreateAuthenticatedClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Full flow test:
    ///   Step 1 – verify /api/investments returns expected investments
    ///   Step 2 – compute expected portfolio value using known stub rates
    ///   Step 3 – assert /api/portfolio/value returns computed expected values
    /// </summary>
    [Theory]
    [InlineData("USD")]
    [InlineData("HUF")]
    public async Task GetPortfolioValue_MatchesComputedExpectedValues(string currency)
    {
        // Step 1: verify investments are seeded correctly
        var investmentsResp = await _client.GetAsync("/api/investments?pageNumber=1&pageSize=50");
        investmentsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var investments = await investmentsResp.Content.ReadFromJsonAsync<GetInvestmentsResponse>(TestJsonOptions.Default);
        investments!.TotalCount.Should().Be(2);

        // Step 2: compute expected values (StubCurrencyConversionService: USD=1.0, HUF=370.50)
        var (expectedAccountBalance, expectedInvestmentValue, expectedTotal) = currency == "USD"
            ? (TestDataSeeder.ExpectedAccountBalanceUsd,
               TestDataSeeder.ExpectedInvestmentValueUsd,
               TestDataSeeder.ExpectedTotalPortfolioUsd)
            : (TestDataSeeder.ExpectedAccountBalanceHuf,
               TestDataSeeder.ExpectedInvestmentValueHuf,
               TestDataSeeder.ExpectedTotalPortfolioHuf);

        // Step 3: call endpoint and assert
        var response = await _client.GetAsync($"/api/portfolio/value?currency={currency}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PortfolioValueResponse>();
        body.Should().NotBeNull();
        body!.Currency.Should().Be(currency);
        body.TotalAccountBalance.Should().BeApproximately(expectedAccountBalance, 0.02m);
        body.TotalInvestmentValue.Should().BeApproximately(expectedInvestmentValue, 0.02m);
        body.TotalPortfolioValue.Should().BeApproximately(expectedTotal, 0.02m);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("HUF")]
    public async Task GetPortfolioValue_AccountBreakdownContainsBothAccounts(string currency)
    {
        var response = await _client.GetAsync($"/api/portfolio/value?currency={currency}");
        var body = await response.Content.ReadFromJsonAsync<PortfolioValueResponse>();

        body!.AccountBreakdown.Should().HaveCount(2);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("HUF")]
    public async Task GetPortfolioValue_InvestmentBreakdownContainsBothSymbols(string currency)
    {
        var response = await _client.GetAsync($"/api/portfolio/value?currency={currency}");
        var body = await response.Content.ReadFromJsonAsync<PortfolioValueResponse>();

        body!.InvestmentBreakdown.Should().Contain(i => i.Symbol == "AAPL");
        body.InvestmentBreakdown.Should().Contain(i => i.Symbol == "MSFT");
    }
}
