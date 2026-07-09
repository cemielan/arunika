using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class ArticleKeywordConfiguration : IEntityTypeConfiguration<ArticleKeyword>
{
    public void Configure(EntityTypeBuilder<ArticleKeyword> builder)
    {
        builder.ToTable("article_keywords");
        builder.HasKey(x => new { x.ArticleId, x.KeywordId });

        builder.HasOne(x => x.Article)
            .WithMany(a => a.Keywords)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Keyword)
            .WithMany(k => k.Articles)
            .HasForeignKey(x => x.KeywordId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
