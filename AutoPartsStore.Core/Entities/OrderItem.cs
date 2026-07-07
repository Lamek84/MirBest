namespace AutoPartsStore.Core.Entities;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    // Снимок данных на момент заказа — чтобы старые заказы не "ехали",
    // если цена или название товара позже изменятся.
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
}
