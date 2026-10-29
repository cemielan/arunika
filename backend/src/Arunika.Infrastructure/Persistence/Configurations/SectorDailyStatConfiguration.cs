using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class SectorDailyStatConfiguration : IEntityTypeConfiguration<SectorDailyStat>
{
    public void Configure(EntityTypeBuilder<SectorDailyStat> builder)
    {
        builder.ToTable("sector_daily_stats");
        builder.HasKey(x => new { x.Date, x.SectorId });

        builder.HasOne(x => x.Sector)
            .WithMany()
            .HasForeignKey(x => x.SectorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
