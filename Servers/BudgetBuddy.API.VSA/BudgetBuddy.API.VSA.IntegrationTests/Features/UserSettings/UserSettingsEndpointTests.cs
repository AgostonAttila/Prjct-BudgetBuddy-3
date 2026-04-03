using System.Net;
using System.Net.Http.Json;
using BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BudgetBuddy.API.VSA.IntegrationTests.Features.UserSettings;

[Collection("Integration")]
public class UserSettingsEndpointTests(IntegrationTestWebAppFactory factory) : IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = await AuthenticatedHttpClient.CreateAsync(factory,
            email: $"settings_{Guid.NewGuid()}@test.com");
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GET_UserSettings_Returns200WithUserData()
    {
        var response = await _client.GetAsync("/api/user-settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<UserSettingsResponse>();
        settings.Should().NotBeNull();
        settings!.Email.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PUT_UpdateUserSettings_ValidRequest_Returns200()
    {
        var response = await _client.PutAsJsonAsync("/api/user-settings", new
        {
            FirstName = "Updated",
            LastName = "User",
            PreferredLanguage = "hu-HU",
            DefaultCurrency = "HUF",
            DateFormat = "yyyy-MM-dd"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<UpdateSettingsResponse>();
        result.Should().NotBeNull();
        result!.DefaultCurrency.Should().Be("HUF");
        result.PreferredLanguage.Should().Be("hu-HU");
    }

    [Fact]
    public async Task GET_UserSettings_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.GetAsync("/api/user-settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_UserSettings_WithoutAuth_Returns401()
    {
        var unauthenticatedClient = factory.CreateClient();
        var response = await unauthenticatedClient.PutAsJsonAsync("/api/user-settings", new
        {
            DefaultCurrency = "EUR"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record UserSettingsResponse(string UserId, string Email, string? FirstName, string? LastName, string? PreferredLanguage, string? DefaultCurrency, string? DateFormat);
    private sealed record UpdateSettingsResponse(string Message, string? PreferredLanguage, string? DefaultCurrency, string? DateFormat);
}
