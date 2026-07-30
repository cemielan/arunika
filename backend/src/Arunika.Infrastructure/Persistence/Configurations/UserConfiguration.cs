using Arunika.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Arunika.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(256);
        builder.Property(u => u.Role).IsRequired().HasMaxLength(50);
        builder.Property(u => u.OtpCode).HasMaxLength(10);
        builder.Property(u => u.ResetToken).HasMaxLength(128);

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
