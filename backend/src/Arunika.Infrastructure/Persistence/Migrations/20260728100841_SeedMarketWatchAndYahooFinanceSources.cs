using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMarketWatchAndYahooFinanceSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "news_sources",
                columns: new[] { "Id", "BaseUrl", "IsActive", "Name", "SourceType", "TrustScore" },
                values: new object[] { new Guid("33333333-0000-0000-0000-000000000003"), "https://www.marketwatch.com", true, "MarketWatch", "Rss", 75 });

            migrationBuilder.InsertData(
                table: "news_sources",
                columns: new[] { "Id", "BaseUrl", "IsActive", "Name", "SourceType", "TrustScore" },
                values: new object[] { new Guid("33333333-0000-0000-0000-000000000004"), "https://finance.yahoo.com", true, "Yahoo Finance", "Rss", 70 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000004"));
        }
    }
}
