using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.IntegrationTests.Features.Accounts;

[Collection("Integration")]
public class AccountsEndpointTests(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = await AuthenticatedHttpClient.CreateAsync(factory,
            email: $"accounts_{Guid.NewGuid()}@test.com");
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GET_Accounts_WhenNoAccounts_Returns200WithEmptyList()
    {
        var response = await _client.GetAsync("/api/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var accounts = await response.Content.ReadFromJsonAsync<List<object>>();
        accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task POST_CreateAccount_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/accounts", new
        {
            Name = "My Savings",
            Description = "Emergency fund",
            DefaultCurrencyCode = "HUF",
            InitialBalance = 500000m
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<AccountResponse>();
        created!.Name.Should().Be("My Savings");
        created.DefaultCurrencyCode.Should().Be("HUF");
    }

    [Fact]
    public async Task POST_CreateAccount_EmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/accounts", new
        {
            Name = "",
            Description = "",
            DefaultCurrencyCode = "USD",
            InitialBalance = 0m
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Accounts_AfterCreation_ReturnsCreatedAccount()
    {
        await _client.PostAsJsonAsync("/api/accounts", new
        {
            Name = "Checking",
            Description = "",
            DefaultCurrencyCode = "EUR",
            InitialBalance = 1000m
        });

        var response = await _client.GetAsync("/api/accounts");
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountResponse>>();

        accounts.Should().ContainSingle(a => a.Name == "Checking");
    }

    [Fact]
    public async Task GET_Accounts_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/accounts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record AccountResponse(
        Guid Id,
        string Name,
        string Description,
        string DefaultCurrencyCode,
        decimal InitialBalance);
}
