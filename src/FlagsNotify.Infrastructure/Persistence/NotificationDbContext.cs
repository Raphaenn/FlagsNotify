using FlagsNotify.Domain.Entities;
using FlagsNotify.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FlagsNotify.Infrastructure.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<DeviceRegistration> DeviceRegistrations => Set<DeviceRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DeviceRegistrationConfiguration());
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
}
