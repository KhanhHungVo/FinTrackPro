using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrackPro.Infrastructure.Persistence.Configurations;

public class WatchedSymbolConfiguration : IEntityTypeConfiguration<WatchedSymbol>
{
    public void Configure(EntityTypeBuilder<WatchedSymbol> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Symbol).HasMaxLength(20).IsRequired();
        builder.HasIndex(w => new { w.UserId, w.Symbol }).IsUnique();

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(w => w.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
