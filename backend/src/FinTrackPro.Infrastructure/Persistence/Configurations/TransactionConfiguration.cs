using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrackPro.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(t => t.Currency).HasMaxLength(3).IsRequired().HasDefaultValue("USD");
        builder.Property(t => t.RateToUsd).HasPrecision(18, 8).IsRequired().HasDefaultValue(1.0m);
        builder.Property(t => t.Category).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Note).HasMaxLength(500);
        builder.Property(t => t.BudgetMonth).HasMaxLength(7).IsRequired();
        builder.Property(t => t.Type).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");
        builder.Property(t => t.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()");

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<TransactionCategory>()
               .WithMany()
               .HasForeignKey(t => t.CategoryId)
               .OnDelete(DeleteBehavior.SetNull)
               .IsRequired(false);
    }
}
