using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class SectorConfiguration : IEntityTypeConfiguration<Sector>
{
    // Fixed GUIDs so the seeded rows are deterministic across environments/migrations.
    public static readonly Guid Energy = new("22222222-0000-0000-0000-000000000001");
    public static readonly Guid Financials = new("22222222-0000-0000-0000-000000000002");
    public static readonly Guid Technology = new("22222222-0000-0000-0000-000000000003");
    public static readonly Guid Industrials = new("22222222-0000-0000-0000-000000000004");
    public static readonly Guid Consumer = new("22222222-0000-0000-0000-000000000005");
    public static readonly Guid Healthcare = new("22222222-0000-0000-0000-000000000006");
    public static readonly Guid RealEstate = new("22222222-0000-0000-0000-000000000007");
    public static readonly Guid Materials = new("22222222-0000-0000-0000-000000000008");
    public static readonly Guid Utilities = new("22222222-0000-0000-0000-000000000009");
    public static readonly Guid CommunicationServices = new("22222222-0000-0000-0000-00000000000a");

    public void Configure(EntityTypeBuilder<Sector> builder)
    {
        builder.ToTable("sectors");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => s.Name).IsUnique();

        builder.HasData(
            new Sector { Id = Energy, Name = "Energy" },
            new Sector { Id = Financials, Name = "Financials" },
            new Sector { Id = Technology, Name = "Technology" },
            new Sector { Id = Industrials, Name = "Industrials" },
            new Sector { Id = Consumer, Name = "Consumer" },
            new Sector { Id = Healthcare, Name = "Healthcare" },
            new Sector { Id = RealEstate, Name = "Real Estate" },
            new Sector { Id = Materials, Name = "Materials" },
            new Sector { Id = Utilities, Name = "Utilities" },
            new Sector { Id = CommunicationServices, Name = "Communication Services" }
        );
    }
}
