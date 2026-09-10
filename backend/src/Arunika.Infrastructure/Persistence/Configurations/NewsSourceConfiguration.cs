using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class NewsSourceConfiguration : IEntityTypeConfiguration<NewsSource>
{
    // Fixed GUIDs so the seeded rows are deterministic across environments/migrations.
    // Declared and seeded in descending TrustScore order, which is the order the
    // sources are worth reading in; the GUID values themselves stay on their
    // original allocation sequence because rows already reference them.
    public static readonly Guid FederalReserve = new("33333333-0000-0000-0000-00000000000d");     // 95
    public static readonly Guid WsjMarkets = new("33333333-0000-0000-0000-000000000008");         // 85
    public static readonly Guid ReutersBusinessNews = new("33333333-0000-0000-0000-000000000005"); // 82, inactive
    public static readonly Guid ReutersMarketsNews = new("33333333-0000-0000-0000-000000000006");  // 82, inactive
    public static readonly Guid FinancialModelingPrep = new("33333333-0000-0000-0000-000000000001"); // 80
    public static readonly Guid BbcBusiness = new("33333333-0000-0000-0000-000000000009");        // 80
    public static readonly Guid AntaraEkonomi = new("33333333-0000-0000-0000-000000000011");      // 78
    public static readonly Guid Cnbc = new("33333333-0000-0000-0000-000000000002");               // 75
    public static readonly Guid MarketWatch = new("33333333-0000-0000-0000-000000000003");        // 75
    public static readonly Guid CnbcIndonesia = new("33333333-0000-0000-0000-00000000000e");      // 75
    public static readonly Guid KontanInvestasi = new("33333333-0000-0000-0000-000000000010");    // 72
    public static readonly Guid DetikFinance = new("33333333-0000-0000-0000-00000000000f");       // 70

    // Retired sources. Their rows are kept because articles reference them
    // (Article.SourceId is FK-restricted), but they are seeded inactive and have
    // no fetcher registered in DependencyInjection.
    public static readonly Guid YahooFinance = new("33333333-0000-0000-0000-000000000004");       // 70, dropped
    public static readonly Guid Gdelt = new("33333333-0000-0000-0000-000000000007");              // 60, dropped

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
                Id = FederalReserve,
                Name = "Federal Reserve Press Releases",
                BaseUrl = "https://www.federalreserve.gov",
                SourceType = SourceType.Rss,
                TrustScore = 95,
                IsActive = true
            },
            new NewsSource
            {
                Id = WsjMarkets,
                Name = "WSJ Markets",
                BaseUrl = "https://www.wsj.com",
                SourceType = SourceType.Rss,
                TrustScore = 85,
                IsActive = true
            },
            new NewsSource
            {
                Id = ReutersBusinessNews,
                Name = "Reuters Business News",
                BaseUrl = "https://www.reuters.com",
                SourceType = SourceType.Rss,
                TrustScore = 82,
                IsActive = false
            },
            new NewsSource
            {
                Id = ReutersMarketsNews,
                Name = "Reuters Markets News",
                BaseUrl = "https://www.reuters.com",
                SourceType = SourceType.Rss,
                TrustScore = 82,
                IsActive = false
            },
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
                Id = BbcBusiness,
                Name = "BBC Business",
                BaseUrl = "https://www.bbc.co.uk",
                SourceType = SourceType.Rss,
                TrustScore = 80,
                IsActive = true
            },
            new NewsSource
            {
                Id = AntaraEkonomi,
                Name = "Antara Ekonomi",
                BaseUrl = "https://www.antaranews.com",
                SourceType = SourceType.Rss,
                TrustScore = 78,
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
                Id = CnbcIndonesia,
                Name = "CNBC Indonesia Market",
                BaseUrl = "https://www.cnbcindonesia.com",
                SourceType = SourceType.Rss,
                TrustScore = 75,
                IsActive = true
            },
            new NewsSource
            {
                Id = KontanInvestasi,
                Name = "Kontan Investasi",
                BaseUrl = "https://investasi.kontan.co.id",
                SourceType = SourceType.Rss,
                TrustScore = 72,
                IsActive = true
            },
            new NewsSource
            {
                Id = DetikFinance,
                Name = "Detik Finance",
                BaseUrl = "https://finance.detik.com",
                SourceType = SourceType.Rss,
                TrustScore = 70,
                IsActive = true
            },
            new NewsSource
            {
                Id = YahooFinance,
                Name = "Yahoo Finance",
                BaseUrl = "https://finance.yahoo.com",
                SourceType = SourceType.Rss,
                TrustScore = 70,
                IsActive = false
            },
            new NewsSource
            {
                Id = Gdelt,
                Name = "GDELT",
                BaseUrl = "https://api.gdeltproject.org",
                SourceType = SourceType.Api,
                TrustScore = 60,
                IsActive = false
            });
    }
}
