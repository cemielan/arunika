using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIndonesianTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExecutiveSummaryIndonesian",
                table: "briefings");

            migrationBuilder.DropColumn(
                name: "ImpactRationaleIndonesian",
                table: "article_analyses");

            migrationBuilder.DropColumn(
                name: "SummaryIndonesian",
                table: "article_analyses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExecutiveSummaryIndonesian",
                table: "briefings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpactRationaleIndonesian",
                table: "article_analyses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryIndonesian",
                table: "article_analyses",
                type: "text",
                nullable: true);
        }
    }
}
