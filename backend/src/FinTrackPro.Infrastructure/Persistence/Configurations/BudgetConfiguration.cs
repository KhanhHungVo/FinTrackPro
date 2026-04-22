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
        builder.Property(b => b.LimitAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(b => b.Currency).HasMaxLength(3).IsRequired().HasDefaultValue("USD");
        builder.Property(b => b.RateToUsd).HasPrecision(18, 8).IsRequired().HasDefaultValue(1.0m);
        builder.Property(b => b.Month).HasMaxLength(7).IsRequired();
        builder.Property(b => b.CreatedAt).IsRequired().HasDefaultValueSql("NOW()").ValueGeneratedNever();
        builder.Property(b => b.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()").ValueGeneratedNever();

        builder.HasIndex(b => new { b.UserId, b.Category, b.Month }).IsUnique();

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(b => b.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
