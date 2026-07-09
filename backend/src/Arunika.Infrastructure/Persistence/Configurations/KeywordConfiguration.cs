using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class KeywordConfiguration : IEntityTypeConfiguration<Keyword>
{
    public void Configure(EntityTypeBuilder<Keyword> builder)
    {
        builder.ToTable("keywords");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Text).IsRequired().HasMaxLength(200);
        builder.HasIndex(k => k.Text).IsUnique();
    }
}
