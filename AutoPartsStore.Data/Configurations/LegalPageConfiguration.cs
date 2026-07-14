using AutoPartsStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsStore.Data.Configurations;

public class LegalPageConfiguration : IEntityTypeConfiguration<LegalPage>
{
    public void Configure(EntityTypeBuilder<LegalPage> builder)
    {
        builder.Property(p => p.Key).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Content).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasIndex(p => p.Key).IsUnique();
    }
}
