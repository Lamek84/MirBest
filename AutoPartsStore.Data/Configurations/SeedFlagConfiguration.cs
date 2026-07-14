using AutoPartsStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsStore.Data.Configurations;

public class SeedFlagConfiguration : IEntityTypeConfiguration<SeedFlag>
{
    public void Configure(EntityTypeBuilder<SeedFlag> builder)
    {
        builder.HasKey(f => f.Key);
        builder.Property(f => f.Key).HasMaxLength(100);
    }
}
