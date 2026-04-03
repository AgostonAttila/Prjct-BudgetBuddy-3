namespace BudgetBuddy.API.VSA.IntegrationTests.Infrastructure;

/// <summary>
/// Shares a single IntegrationTestWebAppFactory (and its containers) across all tests
/// in the "Integration" collection — avoids spinning up Docker containers per test class.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationCollectionFixture : ICollectionFixture<IntegrationTestWebAppFactory>;
