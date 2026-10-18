using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlagsNotify.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DeviceRegistrationConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());
    }

    private sealed class DeviceRegistrationConfiguration
        : IEntityTypeConfiguration<DeviceRegistration>
    {
        public void Configure(EntityTypeBuilder<DeviceRegistration> builder)
        {
            builder.ToTable("user_push_devices");
            builder.HasKey(device => device.Id);

            builder.Property(device => device.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            builder.Property(device => device.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(device => device.Token)
                .HasColumnName("expo_push_token")
                .HasMaxLength(256)
                .HasConversion(
                    token => token.Value,
                    value => ExpoPushToken.Create(value))
                .IsRequired();

            builder.Property(device => device.IsActive)
                .HasColumnName("is_active")
                .IsRequired();

            builder.Property(device => device.RegisteredAt)
                .HasColumnName("registered_at")
                .IsRequired();

            builder.Property(device => device.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.HasIndex(device => device.Token).IsUnique();
            builder.HasIndex(device => new { device.UserId, device.IsActive });
        }
    }

    private sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("notifications");
            builder.HasKey(notification => notification.Id);

            builder.Property(notification => notification.Id)
                .HasColumnName("id")
                .HasColumnType("uuid");

            builder.Property(notification => notification.EventId)
                .HasColumnName("event_id")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(notification => notification.RecipientUserId)
                .HasColumnName("recipient_user_id")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(notification => notification.SenderUserId)
                .HasColumnName("sender_user_id")
                .HasMaxLength(200);

            builder.Property(notification => notification.SenderUserName)
                .HasColumnName("sender_user_name")
                .HasMaxLength(200);

            builder.Property(notification => notification.EventType)
                .HasColumnName("event_type")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(notification => notification.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(notification => notification.Body)
                .HasColumnName("body")
                .HasMaxLength(4_000)
                .IsRequired();

            builder.Property(notification => notification.Route)
                .HasColumnName("route")
                .HasMaxLength(500);

            builder.Property(notification => notification.OccurredAt)
                .HasColumnName("occurred_at")
                .IsRequired();

            builder.Property(notification => notification.ReadAt)
                .HasColumnName("read_at");

            builder.Property(notification => notification.DeliveryCompletedAt)
                .HasColumnName("delivery_completed_at");

            builder.Ignore(notification => notification.IsRead);

            builder.HasIndex(notification => notification.EventId).IsUnique();
            builder.HasIndex(notification => new
            {
                notification.RecipientUserId,
                notification.OccurredAt,
                notification.Id
            });
            builder.HasIndex(notification => new
            {
                notification.RecipientUserId,
                notification.ReadAt
            });
        }
    }
}
