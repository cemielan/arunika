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
            });
    }
}
