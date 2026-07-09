using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleEnrichmentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnrichmentStatus",
                table: "articles",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnrichmentStatus",
                table: "articles");
        }
    }
}
