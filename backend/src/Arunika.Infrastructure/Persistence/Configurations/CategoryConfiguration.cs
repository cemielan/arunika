using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    // Fixed GUIDs so the seeded rows are deterministic across environments/migrations.
    public static readonly Guid Politics = new("11111111-0000-0000-0000-000000000001");
    public static readonly Guid Economy = new("11111111-0000-0000-0000-000000000002");
    public static readonly Guid Markets = new("11111111-0000-0000-0000-000000000003");
    public static readonly Guid Banking = new("11111111-0000-0000-0000-000000000004");
    public static readonly Guid Technology = new("11111111-0000-0000-0000-000000000005");
    public static readonly Guid Commodities = new("11111111-0000-0000-0000-000000000006");
    public static readonly Guid Crypto = new("11111111-0000-0000-0000-000000000007");

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasData(
            new Category { Id = Politics, Name = "Politics" },
            new Category { Id = Economy, Name = "Economy" },
            new Category { Id = Markets, Name = "Markets" },
            new Category { Id = Banking, Name = "Banking" },
            new Category { Id = Technology, Name = "Technology" },
            new Category { Id = Commodities, Name = "Commodities" },
            new Category { Id = Crypto, Name = "Crypto" }
        );
    }
}
