using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrackPro.Infrastructure.Persistence.Configurations;

public class TransactionCategoryConfiguration : IEntityTypeConfiguration<TransactionCategory>
{
    public void Configure(EntityTypeBuilder<TransactionCategory> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Slug).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LabelEn).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LabelVi).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Icon).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Type).IsRequired();
        builder.Property(c => c.IsSystem).IsRequired().HasDefaultValue(false);
        builder.Property(c => c.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(c => c.SortOrder).IsRequired().HasDefaultValue(0);
        builder.Property(c => c.CreatedAt).IsRequired().HasDefaultValueSql("NOW()").ValueGeneratedNever();
        builder.Property(c => c.UpdatedAt).IsRequired().HasDefaultValueSql("NOW()").ValueGeneratedNever();

        builder.HasIndex(c => new { c.UserId, c.Slug }).IsUnique();

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(c => c.UserId)
               .OnDelete(DeleteBehavior.Cascade)
               .IsRequired(false);
    }
}
