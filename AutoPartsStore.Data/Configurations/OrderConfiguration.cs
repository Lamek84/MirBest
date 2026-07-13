using AutoPartsStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsStore.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.Property(o => o.UserId).IsRequired().HasMaxLength(450);
        builder.Property(o => o.Status).IsRequired().HasMaxLength(50);
        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(o => o.PaymentProvider).HasMaxLength(50);
        builder.Property(o => o.PaymentSessionId).HasMaxLength(200);
        // Nullable — старые заказы (из БД до появления доставки) при миграции
        // получат NULL вместо падения на NOT NULL constraint.
        builder.Property(o => o.DeliveryMethod).HasMaxLength(50);
        builder.Property(o => o.DeliveryLabel).HasMaxLength(200);
        builder.Property(o => o.DeliveryCost).HasColumnType("decimal(18,2)");
        builder.Property(o => o.PointsDiscount).HasColumnType("decimal(18,2)");

        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.PaymentSessionId);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
