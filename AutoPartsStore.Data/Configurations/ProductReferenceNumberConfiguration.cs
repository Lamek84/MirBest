using AutoPartsStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsStore.Data.Configurations;

public class ProductReferenceNumberConfiguration : IEntityTypeConfiguration<ProductReferenceNumber>
{
    public void Configure(EntityTypeBuilder<ProductReferenceNumber> builder)
    {
        builder.Property(r => r.Number).IsRequired().HasMaxLength(100);
        builder.Property(r => r.NormalizedNumber).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Brand).HasMaxLength(100);
        // Enum хранится как int.
        builder.Property(r => r.Type).HasConversion<int>();

        // Основной индекс поиска: по нормализованному номеру находим товары.
        builder.HasIndex(r => r.NormalizedNumber);

        // Один и тот же номер одного типа не должен дублироваться у товара.
        builder.HasIndex(r => new { r.ProductId, r.Type, r.NormalizedNumber }).IsUnique();

        // Номера — собственные данные товара: удаление товара их чистит.
        builder.HasOne(r => r.Product)
            .WithMany(p => p.ReferenceNumbers)
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
