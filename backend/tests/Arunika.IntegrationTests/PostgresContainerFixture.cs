using Arunika.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Arunika.IntegrationTests;

/// <summary>
/// Spins up a real, throwaway Postgres 16 container (via Testcontainers) once
/// per test collection and applies the real EF Core migrations against it —
/// including the pg_trgm extension and trigram index the Phase 4 dedup check
/// depends on. <c>ArticleRepository</c>'s dedup/ranking logic uses
/// Postgres-specific functions (<c>EF.Functions.TrigramsSimilarity</c>) that have
/// no translation against the InMemory/Sqlite providers, so a real Postgres is
/// needed to test that logic meaningfully (design doc §13).
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public string ConnectionString => _container.GetConnectionString();

    public ArunikaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ArunikaDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new ArunikaDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "Postgres";
}
