using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arunika.Infrastructure.Persistence.Migrations
{
    public partial class SeedReutersAndGdeltSources : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "news_sources",
                columns: new[] { "Id", "BaseUrl", "IsActive", "Name", "SourceType", "TrustScore" },
                values: new object[] { new Guid("33333333-0000-0000-0000-000000000005"), "https://www.reuters.com", true, "Reuters Business News", "Rss", 82 });

            migrationBuilder.InsertData(
                table: "news_sources",
                columns: new[] { "Id", "BaseUrl", "IsActive", "Name", "SourceType", "TrustScore" },
                values: new object[] { new Guid("33333333-0000-0000-0000-000000000006"), "https://www.reuters.com", true, "Reuters Markets News", "Rss", 82 });

            migrationBuilder.InsertData(
                table: "news_sources",
                columns: new[] { "Id", "BaseUrl", "IsActive", "Name", "SourceType", "TrustScore" },
                values: new object[] { new Guid("33333333-0000-0000-0000-000000000007"), "https://api.gdeltproject.org", true, "GDELT", "Api", 60 });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000007"));
        }
    }
}