using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;

/// <summary>
/// Helpers to get an authenticated HttpClient by registering a test user and logging in.
/// </summary>
public static class AuthenticatedHttpClient
{
    public static async Task<HttpClient> CreateAsync(
        IntegrationTestWebAppFactory factory,
        string email = "test@budgetbuddy.com",
        string password = "Test@1234!")
    {
        var client = factory.CreateClient();

        // Register
        await client.PostAsJsonAsync("/auth/register", new
        {
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "User"
        });

        // Login
        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult?.AccessToken);

        return client;
    }

    private sealed record LoginResult(string AccessToken, string RefreshToken);
}
