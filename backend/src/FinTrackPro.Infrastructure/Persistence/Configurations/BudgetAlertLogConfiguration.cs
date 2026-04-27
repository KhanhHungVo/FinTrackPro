using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrackPro.Infrastructure.Persistence.Configurations;

public class BudgetAlertLogConfiguration : IEntityTypeConfiguration<BudgetAlertLog>
{
    public void Configure(EntityTypeBuilder<BudgetAlertLog> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Category).HasMaxLength(100).IsRequired();
        builder.Property(b => b.Month).HasMaxLength(7).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired().HasDefaultValueSql("NOW()").ValueGeneratedNever();

        builder.HasIndex(b => new { b.UserId, b.Category, b.Month }).IsUnique();

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(b => b.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
