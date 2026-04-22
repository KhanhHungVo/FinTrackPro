using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrackPro.Infrastructure.Persistence.Configurations;

public class SignalConfiguration : IEntityTypeConfiguration<Signal>
{
    public void Configure(EntityTypeBuilder<Signal> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Symbol).HasMaxLength(20).IsRequired();
        builder.Property(s => s.SignalType).IsRequired();
        builder.Property(s => s.Message).HasMaxLength(500).IsRequired();
        builder.Property(s => s.Value).HasPrecision(18, 8);
        builder.Property(s => s.Timeframe).HasMaxLength(10);
        builder.Property(s => s.CreatedAt).IsRequired().HasDefaultValueSql("NOW()").ValueGeneratedNever();
        builder.HasIndex(s => new { s.UserId, s.CreatedAt });

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
