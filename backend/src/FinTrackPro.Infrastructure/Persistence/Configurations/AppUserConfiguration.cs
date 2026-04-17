using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
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
        builder.Property(u => u.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(u => u.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.HasIndex(u => u.Email);

        // Subscription
        builder.Property(u => u.Plan).IsRequired().HasDefaultValue(SubscriptionPlan.Free);
        builder.Property(u => u.PaymentCustomerId).HasMaxLength(100);
        builder.Property(u => u.PaymentSubscriptionId).HasMaxLength(100);
        builder.HasIndex(u => u.PaymentCustomerId).HasDatabaseName("IX_AppUsers_PaymentCustomerId");
    }
}
