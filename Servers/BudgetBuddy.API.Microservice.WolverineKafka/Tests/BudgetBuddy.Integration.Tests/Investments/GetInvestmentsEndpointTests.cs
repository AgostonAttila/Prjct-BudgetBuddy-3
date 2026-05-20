using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.Integration.Tests.Infrastructure;
using BudgetBuddy.Service.Investments.Features.GetInvestments;
using FluentAssertions;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Investments;

[Collection(nameof(InvestmentsApiCollection))]
public class GetInvestmentsEndpointTests(InvestmentsApiFactory factory) : IAsyncLifetime
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

    [Fact]
    public async Task GetInvestments_ReturnsSeededInvestments()
    {
        var response = await _client.GetAsync("/api/investments?pageNumber=1&pageSize=50");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<GetInvestmentsResponse>(TestJsonOptions.Default);
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(2);
        body.Investments.Should().Contain(i => i.Symbol == "AAPL" && i.Quantity == TestDataSeeder.AaplQuantity);
        body.Investments.Should().Contain(i => i.Symbol == "MSFT" && i.Quantity == TestDataSeeder.MsftQuantity);
    }

    [Fact]
    public async Task GetInvestments_ReturnsCorrectPurchasePrices()
    {
        var response = await _client.GetAsync("/api/investments?pageNumber=1&pageSize=50");
        var body = await response.Content.ReadFromJsonAsync<GetInvestmentsResponse>(TestJsonOptions.Default);

        var aapl = body!.Investments.Single(i => i.Symbol == "AAPL");
        aapl.PurchasePrice.Should().Be(TestDataSeeder.AaplPurchasePrice);
        aapl.CurrencyCode.Should().Be("USD");

        var msft = body.Investments.Single(i => i.Symbol == "MSFT");
        msft.PurchasePrice.Should().Be(TestDataSeeder.MsftPurchasePrice);
        msft.CurrencyCode.Should().Be("USD");
    }
}
