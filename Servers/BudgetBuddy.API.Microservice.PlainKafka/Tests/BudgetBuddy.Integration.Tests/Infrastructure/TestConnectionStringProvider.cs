using BudgetBuddy.Shared.Infrastructure.Persistence.ConnectionStrings;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

/// <summary>
/// IConnectionStringProvider implementation that returns a fixed test connection string.
/// Used in WebApplicationFactory to point all DbContexts at the Testcontainers PostgreSQL instance.
/// </summary>
public sealed class TestConnectionStringProvider(string connectionString) : IConnectionStringProvider
{
    public string GetDbConnectionString() => connectionString;
}
