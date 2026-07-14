using AutoPartsStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsStore.Data.Configurations;

public class DetailingPackageConfiguration : IEntityTypeConfiguration<DetailingPackage>
{
    public void Configure(EntityTypeBuilder<DetailingPackage> builder)
    {
        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.ShortDescription).IsRequired().HasMaxLength(300);
        builder.Property(p => p.ImageUrl).IsRequired().HasMaxLength(300);

        // Kein HasColumnType("nvarchar(max)") — SQLite (Tests) akzeptiert das
        // "(max)" nicht, EFs Default-Konvention reicht für unbegrenzten Text.
        builder.Property(p => p.Content).IsRequired();
    }
}
