using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class NewsSourceConfiguration : IEntityTypeConfiguration<NewsSource>
{
    // Fixed GUIDs so the seeded rows are deterministic across environments/migrations.
    public static readonly Guid FinancialModelingPrep = new("33333333-0000-0000-0000-000000000001");
    public static readonly Guid Cnbc = new("33333333-0000-0000-0000-000000000002");
    public static readonly Guid MarketWatch = new("33333333-0000-0000-0000-000000000003");
    public static readonly Guid YahooFinance = new("33333333-0000-0000-0000-000000000004");
    public static readonly Guid ReutersBusinessNews = new("33333333-0000-0000-0000-000000000005");
    public static readonly Guid ReutersMarketsNews = new("33333333-0000-0000-0000-000000000006");
    public static readonly Guid Gdelt = new("33333333-0000-0000-0000-000000000007");

    public void Configure(EntityTypeBuilder<NewsSource> builder)
    {
        builder.ToTable("news_sources");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.BaseUrl).IsRequired().HasMaxLength(500);
        builder.Property(s => s.SourceType).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(s => s.Name).IsUnique();

        builder.HasMany(s => s.Articles)
            .WithOne(a => a.Source)
            .HasForeignKey(a => a.SourceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new NewsSource
            {
                Id = FinancialModelingPrep,
                Name = "Financial Modeling Prep",
                BaseUrl = "https://financialmodelingprep.com",
                SourceType = SourceType.Api,
                TrustScore = 80,
                IsActive = true
            },
            new NewsSource
            {
                Id = Cnbc,
                Name = "CNBC",
                BaseUrl = "https://www.cnbc.com",
                SourceType = SourceType.Rss,
                TrustScore = 75,
                IsActive = true
            },
            new NewsSource
            {
                Id = MarketWatch,
                Name = "MarketWatch",
                BaseUrl = "https://www.marketwatch.com",
                SourceType = SourceType.Rss,
                TrustScore = 75,
                IsActive = true
            },
            new NewsSource
            {
                Id = YahooFinance,
                Name = "Yahoo Finance",
                BaseUrl = "https://finance.yahoo.com",
                SourceType = SourceType.Rss,
                TrustScore = 70,
                IsActive = true
            },
            new NewsSource
            {
                Id = ReutersBusinessNews,
                Name = "Reuters Business News",
                BaseUrl = "https://www.reuters.com",
                SourceType = SourceType.Rss,
                TrustScore = 82,
                IsActive = true
            },
            new NewsSource
            {
                Id = ReutersMarketsNews,
                Name = "Reuters Markets News",
                BaseUrl = "https://www.reuters.com",
                SourceType = SourceType.Rss,
                TrustScore = 82,
                IsActive = true
            },
            new NewsSource
            {
                Id = Gdelt,
                Name = "GDELT",
                BaseUrl = "https://api.gdeltproject.org",
                SourceType = SourceType.Api,
                TrustScore = 60,
                IsActive = true
            });
    }
}
