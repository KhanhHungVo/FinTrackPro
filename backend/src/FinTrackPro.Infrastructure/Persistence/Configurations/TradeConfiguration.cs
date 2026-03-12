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
        builder.Property(t => t.EntryPrice).HasColumnType("decimal(18,8)").IsRequired();
        builder.Property(t => t.ExitPrice).HasColumnType("decimal(18,8)").IsRequired();
        builder.Property(t => t.PositionSize).HasColumnType("decimal(18,8)").IsRequired();
        builder.Property(t => t.Fees).HasColumnType("decimal(18,8)").IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(1000);

        // Result is a computed property — not stored in DB
        builder.Ignore(t => t.Result);

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
