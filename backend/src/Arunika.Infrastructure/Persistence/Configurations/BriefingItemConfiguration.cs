using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class BriefingItemConfiguration : IEntityTypeConfiguration<BriefingItem>
{
    public void Configure(EntityTypeBuilder<BriefingItem> builder)
    {
        builder.ToTable("briefing_items");
        builder.HasKey(x => new { x.BriefingId, x.ArticleId });

        builder.HasOne(x => x.Briefing)
            .WithMany(b => b.Items)
            .HasForeignKey(x => x.BriefingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Article)
            .WithMany(a => a.BriefingItems)
            .HasForeignKey(x => x.ArticleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
