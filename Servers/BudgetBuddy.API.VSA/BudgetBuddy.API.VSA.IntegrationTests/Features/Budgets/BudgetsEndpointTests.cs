using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.IntegrationTests.Features.Budgets;

[Collection("Integration")]
public class BudgetsEndpointTests(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;
    private Guid _categoryId;

    public async Task InitializeAsync()
    {
        _client = await AuthenticatedHttpClient.CreateAsync(factory,
            email: $"budgets_{Guid.NewGuid()}@test.com");

        var categoryResponse = await _client.PostAsJsonAsync("/api/categories", new
        {
            Name = "Food",
            Icon = "🍔",
            Color = "#FF5733"
        });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        _categoryId = category!.Id;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GET_Budgets_WhenNoBudgets_Returns200WithEmptyList()
    {
        var response = await _client.GetAsync("/api/budgets");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var budgets = await response.Content.ReadFromJsonAsync<List<object>>();
        budgets.Should().BeEmpty();
    }

    [Fact]
    public async Task POST_CreateBudget_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/budgets", new
        {
            Name = "Monthly Food",
            CategoryId = _categoryId,
            Amount = 50000m,
            CurrencyCode = "HUF",
            Year = 2026,
            Month = 3
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<BudgetResponse>();
        created!.Name.Should().Be("Monthly Food");
        created.Amount.Should().Be(50000m);
        created.CategoryId.Should().Be(_categoryId);
    }

    [Fact]
    public async Task POST_CreateBudget_InvalidCategoryId_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/api/budgets", new
        {
            Name = "Budget",
            CategoryId = Guid.NewGuid(),
            Amount = 10000m,
            CurrencyCode = "HUF",
            Year = 2026,
            Month = 3
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_CreateBudget_EmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/budgets", new
        {
            Name = "",
            CategoryId = _categoryId,
            Amount = 10000m,
            CurrencyCode = "HUF",
            Year = 2026,
            Month = 3
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Budgets_AfterCreation_ReturnsCreatedBudget()
    {
        await _client.PostAsJsonAsync("/api/budgets", new
        {
            Name = "Utilities",
            CategoryId = _categoryId,
            Amount = 20000m,
            CurrencyCode = "HUF",
            Year = 2026,
            Month = 4
        });

        var response = await _client.GetAsync("/api/budgets?year=2026&month=4");
        var budgets = await response.Content.ReadFromJsonAsync<List<BudgetResponse>>();

        budgets.Should().ContainSingle(b => b.Name == "Utilities");
    }

    [Fact]
    public async Task PUT_UpdateBudget_ValidRequest_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/budgets", new
        {
            Name = "Shopping",
            CategoryId = _categoryId,
            Amount = 30000m,
            CurrencyCode = "HUF",
            Year = 2026,
            Month = 5
        });
        var created = await createResponse.Content.ReadFromJsonAsync<BudgetResponse>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/budgets/{created!.Id}", new
        {
            Name = "Shopping Updated",
            Amount = 35000m
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<BudgetResponse>();
        updated!.Name.Should().Be("Shopping Updated");
        updated.Amount.Should().Be(35000m);
    }

    [Fact]
    public async Task DELETE_Budget_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/budgets", new
        {
            Name = "ToDelete",
            CategoryId = _categoryId,
            Amount = 5000m,
            CurrencyCode = "HUF",
            Year = 2026,
            Month = 6
        });
        var created = await createResponse.Content.ReadFromJsonAsync<BudgetResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/budgets/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_BudgetVsActual_Returns200()
    {
        var response = await _client.GetAsync("/api/budgets/vs-actual?year=2026&month=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_Budgets_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/budgets");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record CategoryResponse(Guid Id, string Name, string? Icon, string? Color);
    private sealed record BudgetResponse(Guid Id, string Name, Guid CategoryId, string CategoryName, decimal Amount, string CurrencyCode, int Year, int Month);
}
