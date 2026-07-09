using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class ArticleSectorImpactConfiguration : IEntityTypeConfiguration<ArticleSectorImpact>
{
    public void Configure(EntityTypeBuilder<ArticleSectorImpact> builder)
    {
        builder.ToTable("article_sector_impacts");
        builder.HasKey(x => new { x.ArticleId, x.SectorId });
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(x => x.Article)
            .WithMany(a => a.SectorImpacts)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Sector)
            .WithMany()
            .HasForeignKey(x => x.SectorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
