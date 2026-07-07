namespace AutoPartsStore.Core.Entities;

// Один вариант доставки: код (хранится в Order.DeliveryMethod), название
// для показа клиенту и фиксированная цена. См. статический список в DeliveryOptions.
public class DeliveryOption
{
    public string Code { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
