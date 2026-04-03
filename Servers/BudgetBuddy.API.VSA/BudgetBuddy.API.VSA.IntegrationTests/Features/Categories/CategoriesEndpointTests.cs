using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.IntegrationTests.Features.Categories;

[Collection("Integration")]
public class CategoriesEndpointTests(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = await AuthenticatedHttpClient.CreateAsync(factory,
            email: $"categories_{Guid.NewGuid()}@test.com");
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GET_Categories_WhenNone_Returns200WithEmptyList()
    {
        var response = await _client.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<List<object>>();
        categories.Should().BeEmpty();
    }

    [Fact]
    public async Task POST_CreateCategory_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new
        {
            Name = "Transport",
            Icon = "🚗",
            Color = "#3498DB"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        created!.Name.Should().Be("Transport");
        created.Icon.Should().Be("🚗");
    }

    [Fact]
    public async Task POST_CreateCategory_EmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new
        {
            Name = "",
            Icon = "🚗",
            Color = "#3498DB"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Categories_AfterCreation_ReturnsCreatedCategory()
    {
        await _client.PostAsJsonAsync("/api/categories", new
        {
            Name = "Health",
            Icon = "💊",
            Color = "#2ECC71"
        });

        var response = await _client.GetAsync("/api/categories");
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();

        categories.Should().ContainSingle(c => c.Name == "Health");
    }

    [Fact]
    public async Task PUT_UpdateCategory_ValidRequest_Returns200()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/categories", new
        {
            Name = "Entertainment",
            Icon = "🎬",
            Color = "#9B59B6"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var updateResponse = await _client.PutAsJsonAsync($"/api/categories/{created!.Id}", new
        {
            Name = "Entertainment & Media",
            Icon = "🎬",
            Color = "#8E44AD"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        updated!.Name.Should().Be("Entertainment & Media");
    }

    [Fact]
    public async Task DELETE_Category_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/categories", new
        {
            Name = "ToDelete",
            Icon = "🗑️",
            Color = "#E74C3C"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/categories/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_Categories_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record CategoryResponse(Guid Id, string Name, string? Icon, string? Color);
}
