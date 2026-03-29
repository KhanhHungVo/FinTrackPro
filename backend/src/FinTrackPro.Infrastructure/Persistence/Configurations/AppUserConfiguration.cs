using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrackPro.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).HasMaxLength(200);
        builder.Property(u => u.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.PreferredLanguage).HasMaxLength(10).IsRequired().HasDefaultValue("en");
        builder.Property(u => u.PreferredCurrency).HasMaxLength(3).IsRequired().HasDefaultValue("USD");
        builder.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        builder.HasIndex(u => u.Email);
    }
}
