using AutoPartsStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsStore.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.Property(a => a.Name).IsRequired().HasMaxLength(150);
        builder.Property(a => a.Email).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Phone).IsRequired().HasMaxLength(50);
        builder.Property(a => a.VehicleInfo).HasMaxLength(150);
        builder.Property(a => a.ServiceType).IsRequired().HasMaxLength(100);
        builder.Property(a => a.TimeSlot).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Message).HasMaxLength(2000);
        builder.Property(a => a.Status).IsRequired().HasMaxLength(50);

        builder.HasIndex(a => a.PreferredDate);
    }
}
