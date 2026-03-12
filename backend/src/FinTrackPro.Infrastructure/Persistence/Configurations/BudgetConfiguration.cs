using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrackPro.Infrastructure.Persistence.Configurations;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Category).HasMaxLength(100).IsRequired();
        builder.Property(b => b.LimitAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(b => b.Month).HasMaxLength(7).IsRequired();

        builder.HasIndex(b => new { b.UserId, b.Category, b.Month }).IsUnique();

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(b => b.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
