using AutoPartsStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsStore.Data.Configurations;

public class VehicleMakeConfiguration : IEntityTypeConfiguration<VehicleMake>
{
    public void Configure(EntityTypeBuilder<VehicleMake> builder)
    {
        builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(m => m.Name).IsUnique();
    }
}
