using Arunika.Domain.Entities;
using Arunika.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class BriefingConfiguration : IEntityTypeConfiguration<Briefing>
{
    public void Configure(EntityTypeBuilder<Briefing> builder)
    {
        builder.ToTable("briefings");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.ExecutiveSummary).IsRequired();
        builder.Property(b => b.OverallSentiment).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.RiskLevel).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(b => b.BriefingDate).IsUnique();
    }
}
