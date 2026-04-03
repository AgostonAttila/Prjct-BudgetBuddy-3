using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.IntegrationTests.Features.BudgetAlerts;

[Collection("Integration")]
public class BudgetAlertsEndpointTests(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = await AuthenticatedHttpClient.CreateAsync(factory,
            email: $"alerts_{Guid.NewGuid()}@test.com");
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GET_BudgetAlerts_Returns200()
    {
        var response = await _client.GetAsync("/api/budget-alerts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BudgetAlertsResponse>();
        result.Should().NotBeNull();
        result!.TotalAlerts.Should().Be(0);
    }

    [Fact]
    public async Task GET_BudgetAlerts_WithYearMonth_Returns200()
    {
        var response = await _client.GetAsync("/api/budget-alerts?year=2026&month=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BudgetAlertsResponse>();
        result.Should().NotBeNull();
        result!.Year.Should().Be(2026);
        result.Month.Should().Be(3);
    }

    [Fact]
    public async Task GET_BudgetAlerts_WithBudgetsAndTransactions_ReturnsAlerts()
    {
        // Create a category
        var catResponse = await _client.PostAsJsonAsync("/api/categories", new
        {
            Name = "Test Food",
            Icon = "🍔",
            Color = "#FF5733"
        });
        var category = await catResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        // Create a budget with a small amount
        await _client.PostAsJsonAsync("/api/budgets", new
        {
            Name = "Tiny Budget",
            CategoryId = category!.Id,
            Amount = 100m,
            CurrencyCode = "HUF",
            Year = 2026,
            Month = 3
        });

        // Create account and transaction exceeding the budget
        var accountResponse = await _client.PostAsJsonAsync("/api/accounts", new
        {
            Name = "Test Account",
            Description = "",
            DefaultCurrencyCode = "HUF",
            InitialBalance = 10000m
        });
        var account = await accountResponse.Content.ReadFromJsonAsync<AccountResponse>();

        await _client.PostAsJsonAsync("/api/transactions", new
        {
            AccountId = account!.Id,
            CategoryId = category.Id,
            Amount = 500m,
            CurrencyCode = "HUF",
            TransactionType = "Expense",
            PaymentType = "Cash",
            TransactionDate = "2026-03-10",
            IsTransfer = false
        });

        var response = await _client.GetAsync("/api/budget-alerts?year=2026&month=3");
        var result = await response.Content.ReadFromJsonAsync<BudgetAlertsResponse>();

        result!.TotalAlerts.Should().BeGreaterThan(0);
        result.ExceededCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GET_BudgetAlerts_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/budget-alerts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record CategoryResponse(Guid Id, string Name, string? Icon, string? Color);
    private sealed record AccountResponse(Guid Id, string Name, string Description, string DefaultCurrencyCode, decimal InitialBalance);
    private sealed record BudgetAlertsResponse(int Year, int Month, int TotalAlerts, int WarningCount, int ExceededCount, List<object>? Alerts);
}
