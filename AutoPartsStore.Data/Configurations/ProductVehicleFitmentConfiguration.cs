using AutoPartsStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsStore.Data.Configurations;

public class ProductVehicleFitmentConfiguration : IEntityTypeConfiguration<ProductVehicleFitment>
{
    public void Configure(EntityTypeBuilder<ProductVehicleFitment> builder)
    {
        builder.HasIndex(f => new { f.ProductId, f.VehicleModelId, f.YearFrom, f.YearTo }).IsUnique();

        // Удаление товара чистит его же записи совместимости — это его собственные данные.
        builder.HasOne(f => f.Product)
            .WithMany()
            .HasForeignKey(f => f.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict — нельзя удалить модель, пока к ней привязаны товары (см. VehicleModelsController.DeleteConfirmed).
        builder.HasOne(f => f.VehicleModel)
            .WithMany()
            .HasForeignKey(f => f.VehicleModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
