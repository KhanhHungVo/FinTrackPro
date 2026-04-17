using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrackPro.Infrastructure.Persistence.Configurations;

public class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(t => t.Direction).IsRequired();
        builder.Property(t => t.Status).IsRequired();
        builder.Property(t => t.EntryPrice).HasPrecision(18, 8).IsRequired();
        builder.Property(t => t.ExitPrice).HasPrecision(18, 8);
        builder.Property(t => t.CurrentPrice).HasPrecision(18, 8);
        builder.Property(t => t.PositionSize).HasPrecision(18, 8).IsRequired();
        builder.Property(t => t.Fees).HasPrecision(18, 8).IsRequired();
        builder.Property(t => t.Currency).HasMaxLength(3).IsRequired().HasDefaultValue("USD");
        builder.Property(t => t.RateToUsd).HasPrecision(18, 8).IsRequired().HasDefaultValue(1.0m);
        builder.Property(t => t.Notes).HasMaxLength(1000);
        builder.Property(t => t.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(t => t.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");

        // Computed properties — not stored in DB
        builder.Ignore(t => t.Result);
        builder.Ignore(t => t.UnrealizedResult);

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
