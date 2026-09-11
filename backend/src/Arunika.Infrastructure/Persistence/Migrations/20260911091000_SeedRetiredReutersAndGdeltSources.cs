using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Inserts the three retired sources that <c>NewsSourceConfiguration.HasData</c>
    /// declares but no database ever received.
    ///
    /// Their original migration (<c>20260804193000_SeedReutersAndGdeltSources</c>)
    /// was hand-written without the <c>[Migration]</c> attribute, so EF never
    /// discovered it and never applied it — <c>dotnet ef migrations list</c> did
    /// not even print it. <c>SeedFreeRssSourcesByTrustScore</c> then tried to
    /// deactivate the same three rows, and an UPDATE matching nothing is a silent
    /// no-op, so the gap never surfaced as an error.
    ///
    /// The rows are seeded inactive to match the configuration: both Reuters feeds
    /// and GDELT are retired, have no fetcher registered in
    /// <see cref="DependencyInjection"/>, and exist only so that
    /// <c>Article.SourceId</c> keeps a valid target. Raw SQL rather than
    /// <c>InsertData</c> so a re-run cannot fail startup migration on a database
    /// that already has them.
    /// </summary>
    [DbContext(typeof(ArunikaDbContext))]
    [Migration("20260911091000_SeedRetiredReutersAndGdeltSources")]
    public partial class SeedRetiredReutersAndGdeltSources : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO news_sources ("Id", "BaseUrl", "IsActive", "Name", "SourceType", "TrustScore")
                VALUES
                    ('33333333-0000-0000-0000-000000000005', 'https://www.reuters.com', false, 'Reuters Business News', 'Rss', 82),
                    ('33333333-0000-0000-0000-000000000006', 'https://www.reuters.com', false, 'Reuters Markets News', 'Rss', 82),
                    ('33333333-0000-0000-0000-000000000007', 'https://api.gdeltproject.org', false, 'GDELT', 'Api', 60)
                ON CONFLICT DO NOTHING;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only safe while nothing references them; articles are FK-restricted.
            migrationBuilder.Sql("""
                DELETE FROM news_sources
                WHERE "Id" IN (
                    '33333333-0000-0000-0000-000000000005',
                    '33333333-0000-0000-0000-000000000006',
                    '33333333-0000-0000-0000-000000000007'
                )
                AND NOT EXISTS (SELECT 1 FROM articles WHERE articles."SourceId" = news_sources."Id");
                """);
        }
    }
}
