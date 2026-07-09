using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedFinancialModelingPrepSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "news_sources",
                columns: new[] { "Id", "BaseUrl", "IsActive", "Name", "SourceType", "TrustScore" },
                values: new object[] { new Guid("33333333-0000-0000-0000-000000000001"), "https://financialmodelingprep.com", true, "Financial Modeling Prep", "Api", 80 });

            migrationBuilder.CreateIndex(
                name: "IX_news_sources_Name",
                table: "news_sources",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_news_sources_Name",
                table: "news_sources");

            migrationBuilder.DeleteData(
                table: "news_sources",
                keyColumn: "Id",
                keyValue: new Guid("33333333-0000-0000-0000-000000000001"));
        }
    }
}
