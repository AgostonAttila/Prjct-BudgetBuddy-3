using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace BudgetBuddy.Integration.Tests.Infrastructure;

/// <summary>
/// xUnit fixture: egyetlen PostgreSQL konténert indít a teljes test futáshoz.
/// Használat: [Collection(nameof(PostgresCollection))]
///
/// A-23: Respawn integráció — <see cref="ResetAsync"/> törli az összes sort (TRUNCATE + RESTART IDENTITY)
/// az adatbázisból minden teszt előtt, így a tesztek izoláltak maradnak külön konténer nélkül.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private Respawner _respawner = null!;

    public string ConnectionString => _postgres.GetConnectionString();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    /// <summary>
    /// Resets all tables to an empty state between tests.
    /// Call from <c>IAsyncLifetime.InitializeAsync</c> of each test class — after migrations have run.
    /// The Respawner is created lazily here so it picks up the current schema.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        // A-23: Respawner is initialised lazily (after migrations) so it can discover the schema.
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
        });

        await _respawner.ResetAsync(connection);
    }

    public DbContextOptions<T> CreateOptions<T>(string schema = "test") where T : DbContext
    {
        return new DbContextOptionsBuilder<T>()
            .UseNpgsql(ConnectionString, o => o.UseNodaTime())
            .Options;
    }
}

[CollectionDefinition(nameof(PostgresCollection))]
public class PostgresCollection : ICollectionFixture<PostgresFixture>;
