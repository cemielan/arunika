using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class ArticleConfiguration : IEntityTypeConfiguration<Article>
{
    public void Configure(EntityTypeBuilder<Article> builder)
    {
        builder.ToTable("articles");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Url).IsRequired().HasMaxLength(1000);
        builder.Property(a => a.RawContent).IsRequired();
        builder.Property(a => a.DedupeHash).IsRequired().HasMaxLength(64);
        builder.Property(a => a.EnrichmentStatus).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(a => a.Url).IsUnique();
        builder.HasIndex(a => a.PublishedAt);
        builder.HasIndex(a => a.DedupeHash);

        // Speeds up the trigram similarity lookup in ArticleRepository.FindDuplicateAsync
        // (design doc §6, Phase 4 near-duplicate check).
        builder.HasIndex(a => a.Title)
            .HasMethod("GIN")
            .HasOperators("gin_trgm_ops");

        builder.HasOne(a => a.DuplicateOf)
            .WithMany()
            .HasForeignKey(a => a.DuplicateOfId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Analysis)
            .WithOne(an => an.Article)
            .HasForeignKey<ArticleAnalysis>(an => an.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
