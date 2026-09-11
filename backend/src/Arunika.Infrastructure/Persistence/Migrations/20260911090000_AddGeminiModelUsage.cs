using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Backing table for <see cref="AI.GeminiUsageStore"/>. Written to by raw SQL
    /// rather than the DbContext — the counter needs a single atomic
    /// insert-or-conditional-increment, which EF change tracking cannot express —
    /// so there is deliberately no entity, configuration or snapshot entry for it.
    /// </summary>
    [DbContext(typeof(ArunikaDbContext))]
    [Migration("20260911090000_AddGeminiModelUsage")]
    public partial class AddGeminiModelUsage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS gemini_model_usage (
                    model text NOT NULL,
                    usage_date date NOT NULL,
                    call_count integer NOT NULL,
                    CONSTRAINT "PK_gemini_model_usage" PRIMARY KEY (model, usage_date)
                );
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS gemini_model_usage;");
        }
    }
}
