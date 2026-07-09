using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Arunika.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "briefings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BriefingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "text", nullable: false),
                    OverallSentiment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_briefings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "keywords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_keywords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "news_sources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TrustScore = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_news_sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RawContent = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FetchedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DedupeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DuplicateOfId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_articles_articles_DuplicateOfId",
                        column: x => x.DuplicateOfId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_articles_news_sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "news_sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "article_analyses",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sentiment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SentimentConfidence = table.Column<float>(type: "real", nullable: false),
                    ImpactScore = table.Column<int>(type: "integer", nullable: false),
                    ImpactRationale = table.Column<string>(type: "text", nullable: true),
                    ModelVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_analyses", x => x.ArticleId);
                    table.ForeignKey(
                        name: "FK_article_analyses_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_article_analyses_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "article_keywords",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeywordId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_keywords", x => new { x.ArticleId, x.KeywordId });
                    table.ForeignKey(
                        name: "FK_article_keywords_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_article_keywords_keywords_KeywordId",
                        column: x => x.KeywordId,
                        principalTable: "keywords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "article_sector_impacts",
                columns: table => new
                {
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Magnitude = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_sector_impacts", x => new { x.ArticleId, x.SectorId });
                    table.ForeignKey(
                        name: "FK_article_sector_impacts_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_article_sector_impacts_sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "briefing_items",
                columns: table => new
                {
                    BriefingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_briefing_items", x => new { x.BriefingId, x.ArticleId });
                    table.ForeignKey(
                        name: "FK_briefing_items_articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_briefing_items_briefings_BriefingId",
                        column: x => x.BriefingId,
                        principalTable: "briefings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000001"), "Politics" },
                    { new Guid("11111111-0000-0000-0000-000000000002"), "Economy" },
                    { new Guid("11111111-0000-0000-0000-000000000003"), "Markets" },
                    { new Guid("11111111-0000-0000-0000-000000000004"), "Banking" },
                    { new Guid("11111111-0000-0000-0000-000000000005"), "Technology" },
                    { new Guid("11111111-0000-0000-0000-000000000006"), "Commodities" },
                    { new Guid("11111111-0000-0000-0000-000000000007"), "Crypto" }
                });

            migrationBuilder.InsertData(
                table: "sectors",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("22222222-0000-0000-0000-000000000001"), "Energy" },
                    { new Guid("22222222-0000-0000-0000-000000000002"), "Financials" },
                    { new Guid("22222222-0000-0000-0000-000000000003"), "Technology" },
                    { new Guid("22222222-0000-0000-0000-000000000004"), "Industrials" },
                    { new Guid("22222222-0000-0000-0000-000000000005"), "Consumer" },
                    { new Guid("22222222-0000-0000-0000-000000000006"), "Healthcare" },
                    { new Guid("22222222-0000-0000-0000-000000000007"), "Real Estate" },
                    { new Guid("22222222-0000-0000-0000-000000000008"), "Materials" },
                    { new Guid("22222222-0000-0000-0000-000000000009"), "Utilities" },
                    { new Guid("22222222-0000-0000-0000-00000000000a"), "Communication Services" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_article_analyses_CategoryId",
                table: "article_analyses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_article_keywords_KeywordId",
                table: "article_keywords",
                column: "KeywordId");

            migrationBuilder.CreateIndex(
                name: "IX_article_sector_impacts_SectorId",
                table: "article_sector_impacts",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_articles_DedupeHash",
                table: "articles",
                column: "DedupeHash");

            migrationBuilder.CreateIndex(
                name: "IX_articles_DuplicateOfId",
                table: "articles",
                column: "DuplicateOfId");

            migrationBuilder.CreateIndex(
                name: "IX_articles_PublishedAt",
                table: "articles",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_articles_SourceId",
                table: "articles",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_articles_Url",
                table: "articles",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_briefing_items_ArticleId",
                table: "briefing_items",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_briefings_BriefingDate",
                table: "briefings",
                column: "BriefingDate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name",
                table: "categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_keywords_Text",
                table: "keywords",
                column: "Text",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sectors_Name",
                table: "sectors",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_analyses");

            migrationBuilder.DropTable(
                name: "article_keywords");

            migrationBuilder.DropTable(
                name: "article_sector_impacts");

            migrationBuilder.DropTable(
                name: "briefing_items");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "keywords");

            migrationBuilder.DropTable(
                name: "sectors");

            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropTable(
                name: "briefings");

            migrationBuilder.DropTable(
                name: "news_sources");
        }
    }
}
