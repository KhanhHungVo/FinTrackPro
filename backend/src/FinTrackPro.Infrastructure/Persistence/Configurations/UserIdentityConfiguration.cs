using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrackPro.Infrastructure.Persistence.Configurations;

public class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.ExternalUserId).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Provider).HasMaxLength(200).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired().HasDefaultValueSql("NOW()");

        builder.HasIndex(i => new { i.ExternalUserId, i.Provider }).IsUnique();

        builder.HasOne(i => i.User)
               .WithMany(u => u.Identities)
               .HasForeignKey(i => i.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
