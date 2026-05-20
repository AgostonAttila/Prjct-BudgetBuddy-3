using WireMock.Server;
using WireMock.Settings;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

/// <summary>
/// xUnit fixture: egyetlen WireMock.Net szerver fut az összes integrációs teszthez.
/// Használat: [Collection(nameof(WireMockCollection))]
///
/// A-22: Külső HTTP függőségeket (pl. currency exchange API, Keycloak admin REST API)
/// WireMock-kal stubboljuk, így az integrációs tesztek nem igényelnek valódi hálózati kapcsolatot.
///
/// Mintahasználat:
/// <code>
/// WireMock.Given(Request.Create().WithPath("/rates").UsingGet())
///         .RespondWith(Response.Create().WithBodyAsJson(new { EUR = 1.0 }).WithStatusCode(200));
/// </code>
/// </summary>
public sealed class WireMockFixture : IAsyncLifetime
{
    public WireMockServer WireMock { get; private set; } = null!;

    /// <summary>The base URL of the WireMock server (e.g., "http://localhost:12345").</summary>
    public string BaseUrl => WireMock.Urls[0];

    public Task InitializeAsync()
    {
        WireMock = WireMockServer.Start(new WireMockServerSettings
        {
            UseSSL = false,
            StartAdminInterface = false,
        });

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        WireMock.Stop();
        WireMock.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes all registered stubs between tests so each test starts with a clean server.
    /// Call from <c>IAsyncLifetime.InitializeAsync</c> of each test class.
    /// </summary>
    public void Reset() => WireMock.Reset();
}

[CollectionDefinition(nameof(WireMockCollection))]
public class WireMockCollection : ICollectionFixture<WireMockFixture>;
