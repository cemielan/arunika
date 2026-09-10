using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedFreeRssSourcesByTrustScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000004"),
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000005"),
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000006"),
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000007"),
                column: "IsActive",
                value: false);

            migrationBuilder.InsertData(
                table: "news_sources",
                columns: new[] { "Id", "BaseUrl", "IsActive", "Name", "SourceType", "TrustScore" },
                values: new object[,]
                {
                    { new Guid("33333333-0000-0000-0000-000000000008"), "https://www.wsj.com", true, "WSJ Markets", "Rss", 85 },
                    { new Guid("33333333-0000-0000-0000-000000000009"), "https://www.bbc.co.uk", true, "BBC Business", "Rss", 80 },
                    { new Guid("33333333-0000-0000-0000-00000000000d"), "https://www.federalreserve.gov", true, "Federal Reserve Press Releases", "Rss", 95 },
                    { new Guid("33333333-0000-0000-0000-00000000000e"), "https://www.cnbcindonesia.com", true, "CNBC Indonesia Market", "Rss", 75 },
                    { new Guid("33333333-0000-0000-0000-00000000000f"), "https://finance.detik.com", true, "Detik Finance", "Rss", 70 },
                    { new Guid("33333333-0000-0000-0000-000000000010"), "https://investasi.kontan.co.id", true, "Kontan Investasi", "Rss", 72 },
                    { new Guid("33333333-0000-0000-0000-000000000011"), "https://www.antaranews.com", true, "Antara Ekonomi", "Rss", 78 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-00000000000d"));

            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-00000000000e"));

            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-00000000000f"));

            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000011"));

            migrationBuilder.UpdateData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000004"),
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000005"),
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000006"),
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000007"),
                column: "IsActive",
                value: true);
        }
    }
}
