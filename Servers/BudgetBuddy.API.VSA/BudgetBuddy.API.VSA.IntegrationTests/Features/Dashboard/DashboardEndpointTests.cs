using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.IntegrationTests.Features.Dashboard;

[Collection("Integration")]
public class DashboardEndpointTests(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = await AuthenticatedHttpClient.CreateAsync(factory,
            email: $"dashboard_{Guid.NewGuid()}@test.com");
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GET_Dashboard_Returns200()
    {
        var response = await _client.GetAsync("/api/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>();
        dashboard.Should().NotBeNull();
    }

    [Fact]
    public async Task GET_Dashboard_WithDisplayCurrency_Returns200()
    {
        var response = await _client.GetAsync("/api/dashboard?displayCurrency=HUF");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardResponse>();
        dashboard.Should().NotBeNull();
        dashboard!.DisplayCurrency.Should().Be("HUF");
    }

    [Fact]
    public async Task GET_Dashboard_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record DashboardResponse(
        string DisplayCurrency,
        object? AccountsSummary,
        object? CurrentMonthSummary,
        object? BudgetSummary,
        List<object>? TopCategories,
        List<object>? RecentTransactions);
}
