namespace AutoPartsStore.Core.Entities;

public class Order : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    // Простой статус строкой — см. константы в OrderStatus.
    public string Status { get; set; } = OrderStatus.PendingPayment;

    // Сумма товаров + стоимость доставки (DeliveryCost).
    public decimal TotalAmount { get; set; }

    // Снимок выбранного способа доставки на момент заказа — код, название
    // и цена берутся из DeliveryOptions, но хранятся отдельно, чтобы будущие
    // изменения цен/списка служб не меняли задним числом уже оформленные заказы.
    public string? DeliveryMethod { get; set; }
    public string? DeliveryLabel { get; set; }
    public decimal DeliveryCost { get; set; }

    // Кто и по какой сессии принял оплату — нужно, чтобы найти заказ
    // по webhook-уведомлению от платёжного провайдера (см. PaymentsController).
    public string? PaymentProvider { get; set; }
    public string? PaymentSessionId { get; set; }

    // Бонусная программа: сколько баллов списано на скидку в этом заказе
    // (и на какую сумму), и сколько баллов начислено после оплаты.
    // TotalAmount уже учитывает PointsDiscount (уменьшен на эту сумму).
    public int PointsRedeemed { get; set; }
    public decimal PointsDiscount { get; set; }
    public int PointsEarned { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
