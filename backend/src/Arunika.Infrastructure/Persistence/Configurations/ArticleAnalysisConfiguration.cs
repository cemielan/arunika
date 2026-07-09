using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class ArticleAnalysisConfiguration : IEntityTypeConfiguration<ArticleAnalysis>
{
    public void Configure(EntityTypeBuilder<ArticleAnalysis> builder)
    {
        builder.ToTable("article_analyses");
        builder.HasKey(a => a.ArticleId);
        builder.Property(a => a.Summary).IsRequired();
        builder.Property(a => a.Sentiment).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.ModelVersion).IsRequired().HasMaxLength(100);

        builder.HasOne(a => a.Category)
            .WithMany()
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
