using AutoPartsStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsStore.Data.Configurations;

public class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
{
    public void Configure(EntityTypeBuilder<VehicleModel> builder)
    {
        builder.Property(m => m.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(m => new { m.VehicleMakeId, m.Name }).IsUnique();

        // Restrict — нельзя удалить марку, пока у неё есть модели (см. VehicleMakesController.DeleteConfirmed).
        builder.HasOne(m => m.VehicleMake)
            .WithMany(mk => mk.Models)
            .HasForeignKey(m => m.VehicleMakeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
