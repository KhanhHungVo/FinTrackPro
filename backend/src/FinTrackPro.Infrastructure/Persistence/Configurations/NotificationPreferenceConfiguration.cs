using FinTrackPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinTrackPro.Infrastructure.Persistence.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Channel).IsRequired();
        builder.Property(n => n.TelegramChatId).HasMaxLength(100);
        builder.Property(n => n.Email).HasMaxLength(200);
        builder.HasIndex(n => n.UserId).IsUnique();

        builder.HasOne<AppUser>()
               .WithMany()
               .HasForeignKey(n => n.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
