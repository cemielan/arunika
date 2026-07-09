using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedCnbcSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "news_sources",
                columns: new[] { "Id", "BaseUrl", "IsActive", "Name", "SourceType", "TrustScore" },
                values: new object[] { new Guid("33333333-0000-0000-0000-000000000002"), "https://www.cnbc.com", true, "CNBC", "Rss", 75 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000002"));
        }
    }
}
