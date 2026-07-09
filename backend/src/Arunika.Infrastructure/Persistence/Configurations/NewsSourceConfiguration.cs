using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class NewsSourceConfiguration : IEntityTypeConfiguration<NewsSource>
{
    public void Configure(EntityTypeBuilder<NewsSource> builder)
    {
        builder.ToTable("news_sources");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.BaseUrl).IsRequired().HasMaxLength(500);
        builder.Property(s => s.SourceType).HasConversion<string>().HasMaxLength(20);

        builder.HasMany(s => s.Articles)
            .WithOne(a => a.Source)
            .HasForeignKey(a => a.SourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
