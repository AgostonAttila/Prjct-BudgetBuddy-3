using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.IntegrationTests.Features.CategoryTypes;

[Collection("Integration")]
public class CategoryTypesEndpointTests(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;
    private Guid _categoryId;

    public async Task InitializeAsync()
    {
        _client = await AuthenticatedHttpClient.CreateAsync(factory,
            email: $"cattypes_{Guid.NewGuid()}@test.com");

        var catResponse = await _client.PostAsJsonAsync("/api/categories", new
        {
            Name = "Food",
            Icon = "🍔",
            Color = "#FF5733"
        });
        var cat = await catResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        _categoryId = cat!.Id;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GET_CategoryTypes_WhenNone_Returns200()
    {
        var response = await _client.GetAsync($"/api/category-types?categoryId={_categoryId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_CreateCategoryType_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/api/category-types", new
        {
            CategoryId = _categoryId,
            Name = "Groceries",
            Icon = "🛒",
            Color = "#27AE60"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CategoryTypeResponse>();
        created!.Name.Should().Be("Groceries");
        created.CategoryId.Should().Be(_categoryId);
    }

    [Fact]
    public async Task POST_CreateCategoryType_EmptyName_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/category-types", new
        {
            CategoryId = _categoryId,
            Name = "",
            Icon = "🛒",
            Color = "#27AE60"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DELETE_CategoryType_Returns204()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/category-types", new
        {
            CategoryId = _categoryId,
            Name = "ToDelete",
            Icon = "🗑️",
            Color = "#E74C3C"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryTypeResponse>();

        var deleteResponse = await _client.DeleteAsync($"/api/category-types/{created!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GET_CategoryTypes_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/category-types");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record CategoryResponse(Guid Id, string Name, string? Icon, string? Color);
    private sealed record CategoryTypeResponse(Guid Id, Guid CategoryId, string Name, string? Icon, string? Color);
}
