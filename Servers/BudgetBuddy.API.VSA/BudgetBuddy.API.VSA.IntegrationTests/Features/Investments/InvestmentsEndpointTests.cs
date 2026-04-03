using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.IntegrationTests.Features.Investments;

[Collection("Integration")]
public class InvestmentsEndpointTests(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = await AuthenticatedHttpClient.CreateAsync(factory,
            email: $"investments_{Guid.NewGuid()}@test.com");
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GET_Investments_WhenNone_Returns200()
    {
        var response = await _client.GetAsync("/api/investments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GetInvestmentsResponse>();
        result!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task POST_CreateInvestment_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/investments", new
        {
            Symbol = "BTC",
            Name = "Bitcoin",
            Type = "Crypto",
            Quantity = 0.5m,
            PurchasePrice = 10000000m,
            CurrencyCode = "HUF",
            PurchaseDate = "2026-01-15"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<InvestmentResponse>();
        created!.Symbol.Should().Be("BTC");
        created.Type.Should().Be("Crypto");
    }

    [Fact]
    public async Task POST_CreateInvestment_EmptySymbol_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/investments", new
        {
            Symbol = "",
            Name = "Bitcoin",
            Type = "Crypto",
            Quantity = 0.5m,
            PurchasePrice = 10000000m,
            CurrencyCode = "HUF",
            PurchaseDate = "2026-01-15"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Investments_AfterCreation_ReturnsInvestment()
    {
        await _client.PostAsJsonAsync("/api/investments", new
        {
            Symbol = "ETH",
            Name = "Ethereum",
            Type = "Crypto",
            Quantity = 2m,
            PurchasePrice = 500000m,
            CurrencyCode = "HUF",
            PurchaseDate = "2026-02-01"
        });

        var response = await _client.GetAsync("/api/investments");
        var result = await response.Content.ReadFromJsonAsync<GetInvestmentsResponse>();

        result!.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PUT_UpdateInvestment_ValidRequest_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/investments", new
        {
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Type = "Stock",
            Quantity = 10m,
            PurchasePrice = 50000m,
            CurrencyCode = "HUF",
            PurchaseDate = "2026-01-01"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<InvestmentResponse>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/investments/{created!.Id}", new
        {
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Type = "Stock",
            Quantity = 15m,
            PurchasePrice = 50000m,
            CurrencyCode = "HUF",
            PurchaseDate = "2026-01-01"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<InvestmentResponse>();
        updated!.Quantity.Should().Be(15m);
    }

    [Fact]
    public async Task DELETE_Investment_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/investments", new
        {
            Symbol = "TSLA",
            Name = "Tesla",
            Type = "Stock",
            Quantity = 5m,
            PurchasePrice = 30000m,
            CurrencyCode = "HUF",
            PurchaseDate = "2026-01-01"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<InvestmentResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/investments/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_PortfolioValue_Returns200()
    {
        var response = await _client.GetAsync("/api/portfolio/value?currency=HUF");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_Investments_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/investments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record InvestmentResponse(Guid Id, string Symbol, string Name, string Type, decimal Quantity, decimal PurchasePrice, string CurrencyCode);
    private sealed record GetInvestmentsResponse(int TotalCount, int PageNumber, int PageSize, List<object> Investments);
}
