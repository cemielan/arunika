using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBriefingArchiveSectorStatsAndTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AverageImpactScore",
                table: "briefings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BearishCount",
                table: "briefings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BullishCount",
                table: "briefings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExecutiveSummaryIndonesian",
                table: "briefings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NeutralCount",
                table: "briefings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.CreateTable(
                name: "sector_daily_stats",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleCount = table.Column<int>(type: "integer", nullable: false),
                    BullishCount = table.Column<int>(type: "integer", nullable: false),
                    BearishCount = table.Column<int>(type: "integer", nullable: false),
                    NeutralCount = table.Column<int>(type: "integer", nullable: false),
                    AverageImpactScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sector_daily_stats", x => new { x.Date, x.SectorId });
                    table.ForeignKey(
                        name: "FK_sector_daily_stats_sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sector_daily_stats_SectorId",
                table: "sector_daily_stats",
                column: "SectorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sector_daily_stats");

            migrationBuilder.DropColumn(
                name: "AverageImpactScore",
                table: "briefings");

            migrationBuilder.DropColumn(
                name: "BearishCount",
                table: "briefings");

            migrationBuilder.DropColumn(
                name: "BullishCount",
                table: "briefings");

            migrationBuilder.DropColumn(
                name: "ExecutiveSummaryIndonesian",
                table: "briefings");

            migrationBuilder.DropColumn(
                name: "NeutralCount",
                table: "briefings");

            migrationBuilder.DropColumn(
                name: "ImpactRationaleIndonesian",
                table: "article_analyses");

            migrationBuilder.DropColumn(
                name: "SummaryIndonesian",
                table: "article_analyses");
        }
    }
}
